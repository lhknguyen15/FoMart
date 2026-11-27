using System.Globalization;
using System.Text;
using FoMart.Application.DTO;
using FoMart.Application.DTO.Sales;
using FoMart.Application.Interfaces.UnitOfWork;
using FoMart.Domain.Entities;
using FoMart.Domain.Enums;
using FoMart.Domain.Rules;

namespace FoMart.Application.Services.Sales;

// Bán hàng (UC-05) và hủy hóa đơn (UC-06).
// Tiền mặt: lưu là thanh toán ngay và trừ kho. Chuyển khoản: lưu "chờ thanh toán", trừ kho khi bấm "Đã nhận".
// Trừ kho tại vị trí bán mặc định theo FEFO/FIFO (BR-04), cho phép âm (BR-01), giá vốn theo lô bị trừ (BR-06).
public sealed class SaleService(IUnitOfWork unitOfWork)
{
    // Máy bán được lệch giờ tối đa chừng này so với server.
    private static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);

    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    private static readonly CompositeFormat UnitNotFoundFormat = CompositeFormat.Parse(SalesResponseMessageDTO.UnitNotFound);
    private static readonly CompositeFormat UnitInactiveFormat = CompositeFormat.Parse(SalesResponseMessageDTO.UnitInactive);
    private static readonly CompositeFormat ProductInactiveFormat = CompositeFormat.Parse(SalesResponseMessageDTO.ProductInactive);
    private static readonly CompositeFormat UndeclaredFormat = CompositeFormat.Parse(SalesResponseMessageDTO.UndeclaredNeedsNameAndPrice);
    private static readonly CompositeFormat ManualPriceFormat = CompositeFormat.Parse(SalesResponseMessageDTO.DeclaredHasNoManualPrice);
    private static readonly CompositeFormat ExpiredFormat = CompositeFormat.Parse(SalesResponseMessageDTO.ExpiredNeedsConfirm);
    private static readonly CompositeFormat WarnShortFormat = CompositeFormat.Parse(SalesResponseMessageDTO.WarnShort);
    private static readonly CompositeFormat WarnProvisionalFormat = CompositeFormat.Parse(SalesResponseMessageDTO.WarnProvisional);
    private static readonly CompositeFormat WarnExpiredFormat = CompositeFormat.Parse(SalesResponseMessageDTO.WarnExpired);

    // Kết quả trừ kho: lỗi (cần xác nhận lô hết hạn) hoặc danh sách cảnh báo.
    private sealed record DeductOutcome(HTTPResponseData<SaleResponse?>? Error, IReadOnlyList<string> Warnings);

    // Tìm hóa đơn có phân trang (lọc trạng thái, hình thức, người bán, mã, khoảng ngày).
    public async Task<HTTPResponseData<PagedResult<SaleListItemResponse>?>> SearchAsync(
        SaleSearchRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Status) && !SaleStatus.All.Contains(request.Status))
            return HTTPResponse.Fail<PagedResult<SaleListItemResponse>>(400, SalesResponseMessageDTO.InvalidSaleStatus);
        if (!string.IsNullOrWhiteSpace(request.PaymentMethod) && !PaymentMethod.All.Contains(request.PaymentMethod))
            return HTTPResponse.Fail<PagedResult<SaleListItemResponse>>(400, SalesResponseMessageDTO.InvalidPaymentMethod);
        if (request.From > request.To)
            return HTTPResponse.Fail<PagedResult<SaleListItemResponse>>(400, SalesResponseMessageDTO.InvalidDateRange);

        var result = await unitOfWork.SaleRepository.SearchAsync(
            request, StartOfDayUtc(request.From), StartOfDayUtc(request.To?.AddDays(1)), cancellationToken);
        return HTTPResponse.Success(result, SalesResponseMessageDTO.SearchSalesSuccess);
    }

    // Lấy chi tiết hóa đơn kèm các lô đã trừ. Nhân viên không thấy giá vốn, lãi (BR-08).
    public async Task<HTTPResponseData<SaleResponse?>> GetByIdAsync(Guid saleId, string? role, CancellationToken cancellationToken)
    {
        var sale = await unitOfWork.SaleRepository.GetWithLinesAsync(saleId, cancellationToken);
        return sale is null
            ? Fail(404, SalesResponseMessageDTO.SaleNotFound)
            : await BuildResponseAsync(sale, role, [], SalesResponseMessageDTO.GetSaleSuccess, 200, cancellationToken);
    }

    // Lưu hóa đơn (UC-05 bước 5-6). Gửi lại cùng Id thì trả hóa đơn đã ghi, không tạo mới (NFR-05).
    public async Task<HTTPResponseData<SaleResponse?>> CreateAsync(
        CreateSaleRequest request, int userId, string? role, CancellationToken cancellationToken)
    {
        if (request.Id.Version != 7) return Fail(400, SalesResponseMessageDTO.IdMustBeUuidV7);
        if (!PaymentMethod.All.Contains(request.PaymentMethod)) return Fail(400, SalesResponseMessageDTO.InvalidPaymentMethod);

        var existing = await unitOfWork.SaleRepository.GetWithLinesAsync(request.Id, cancellationToken);
        if (existing is not null)
            return existing.CreatedBy == userId
                ? await BuildResponseAsync(existing, role, [], SalesResponseMessageDTO.AlreadyRecorded, 200, cancellationToken)
                : Fail(409, SalesResponseMessageDTO.IdUsedByOtherUser);

        var now = DateTime.UtcNow;
        var createdAt = request.CreatedAt?.ToUniversalTime() ?? now;
        if (createdAt > now + MaxClockSkew) return Fail(400, SalesResponseMessageDTO.CreatedAtInFuture);

        var location = await unitOfWork.LocationRepository.GetDefaultSaleAsync(cancellationToken);
        if (location is null) return Fail(409, SalesResponseMessageDTO.NoSaleLocation);

        var unitIds = request.Lines.Where(l => l.UnitId is not null).Select(l => l.UnitId!.Value).Distinct().ToList();
        var units = await unitOfWork.ProductRepository.GetUnitsWithProductAsync(unitIds, cancellationToken);

        var isCash = request.PaymentMethod == PaymentMethod.Cash;
        var sale = new Sale
        {
            Id = request.Id,
            Status = isCash ? SaleStatus.Paid : SaleStatus.PendingPayment,
            PaymentMethod = request.PaymentMethod,
            LocationId = location.Id,
            CashGiven = request.CashGiven,
            CreatedBy = userId,
            CreatedAt = createdAt,
            PaidAt = isCash ? now : null,
            IsOffline = false,
        };

        foreach (var (lineRequest, index) in request.Lines.Select((l, i) => (l, i)))
        {
            var lineNo = (short)(index + 1);
            var (line, error) = BuildLine(lineRequest, lineNo, units);
            if (error is not null) return error;
            sale.SaleLines.Add(line!);
        }
        sale.Total = sale.SaleLines.Sum(l => l.LineTotal);

        if (request.CashGiven is { } cashGiven)
        {
            if (!isCash) return Fail(400, SalesResponseMessageDTO.CashGivenOnlyForCash);
            if (cashGiven < sale.Total) return Fail(400, SalesResponseMessageDTO.CashGivenTooLow);
        }

        if (!isCash)
        {
            await unitOfWork.SaleRepository.AddAsync(sale, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return await BuildResponseAsync(sale, role, [], SalesResponseMessageDTO.CreatePendingSuccess, 201, cancellationToken);
        }

        await using var write = await unitOfWork.BeginWriteAsync(cancellationToken);
        await unitOfWork.SaleRepository.AddAsync(sale, cancellationToken);
        var outcome = await DeductStockAsync(sale, userId, confirmAllExpired: false, cancellationToken);
        if (outcome.Error is not null) return outcome.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        return await BuildResponseAsync(sale, role, outcome.Warnings, SalesResponseMessageDTO.CreatePaidSuccess, 201, cancellationToken);
    }

    // Người bán bấm "Đã nhận" tiền chuyển khoản: hóa đơn chờ thanh toán chuyển sang đã thanh toán và trừ kho.
    public async Task<HTTPResponseData<SaleResponse?>> ConfirmPaymentAsync(
        Guid saleId, ConfirmSalePaymentRequest request, int userId, string? role, CancellationToken cancellationToken)
    {
        await using var write = await unitOfWork.BeginWriteAsync(cancellationToken);
        var sale = await unitOfWork.SaleRepository.GetWithLinesAsync(saleId, cancellationToken);
        if (sale is null) return Fail(404, SalesResponseMessageDTO.SaleNotFound);
        if (sale.Status != SaleStatus.PendingPayment) return Fail(409, SalesResponseMessageDTO.NotPending);

        sale.Status = SaleStatus.Paid;
        sale.PaidAt = DateTime.UtcNow;
        var outcome = await DeductStockAsync(sale, userId, request.ConfirmExpired, cancellationToken);
        if (outcome.Error is not null) return outcome.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
        return await BuildResponseAsync(sale, role, outcome.Warnings, SalesResponseMessageDTO.ConfirmPaymentSuccess, 200, cancellationToken);
    }

    // Hủy hóa đơn. Chờ thanh toán: ai cũng bỏ được (chưa trừ kho).
    // Đã thanh toán: chỉ chủ tiệm; thêm dòng sổ "sale_cancel" cộng lại đúng các lô đã trừ, hóa đơn gốc giữ nguyên (BR-03, UC-06).
    public async Task<HTTPResponseData<SaleResponse?>> CancelAsync(
        Guid saleId, CancelSaleRequest request, int userId, string? role, CancellationToken cancellationToken)
    {
        await using var write = await unitOfWork.BeginWriteAsync(cancellationToken);
        var sale = await unitOfWork.SaleRepository.GetWithLinesAsync(saleId, cancellationToken);
        if (sale is null) return Fail(404, SalesResponseMessageDTO.SaleNotFound);
        if (sale.Status == SaleStatus.Cancelled) return Fail(409, SalesResponseMessageDTO.AlreadyCancelled);

        var wasPaid = sale.Status == SaleStatus.Paid;
        if (wasPaid && role != UserRole.Owner) return Fail(403, SalesResponseMessageDTO.OnlyOwnerCancelsPaid);

        if (wasPaid)
        {
            var moves = await unitOfWork.StockRepository.GetSaleMovesAsync(saleId, cancellationToken);
            foreach (var move in moves.Where(m => m.DocType == DocType.Sale))
            {
                await unitOfWork.StockRepository.AddAsync(new StockMove
                {
                    ProductId = move.ProductId,
                    LotId = move.LotId,
                    LocationId = move.LocationId,
                    QtyChange = -move.QtyChange,
                    UnitCost = move.UnitCost,
                    DocType = DocType.SaleCancel,
                    SaleLineId = move.SaleLineId,
                    UserId = userId,
                }, cancellationToken);
            }
        }

        sale.Status = SaleStatus.Cancelled;
        sale.CancelledBy = userId;
        sale.CancelledAt = DateTime.UtcNow;
        sale.CancelReason = request.Reason.Trim();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);

        var message = wasPaid ? SalesResponseMessageDTO.CancelPaidSuccess : SalesResponseMessageDTO.CancelPendingSuccess;
        return await BuildResponseAsync(sale, role, [], message, 200, cancellationToken);
    }

    // Dựng một dòng hóa đơn. Hàng đã khai báo: giá và tên lấy theo đơn vị lúc bán (BR-07, không sửa giá).
    private static (SaleLine? Line, HTTPResponseData<SaleResponse?>? Error) BuildLine(
        SaleLineRequest request, short lineNo, IReadOnlyDictionary<int, ProductUnit> units)
    {
        if (request.UnitId is not { } unitId)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.UnitPrice is not { } manualPrice)
                return (null, Fail(400, string.Format(Vietnamese, UndeclaredFormat, lineNo)));
            return (new SaleLine
            {
                LineNo = lineNo,
                NameSnapshot = request.Name.Trim(),
                Qty = request.Qty,
                UnitPrice = manualPrice,
                LineTotal = UnitConverter.LineTotal(manualPrice, request.Qty),
                ExpiredConfirmed = false,
            }, null);
        }

        if (!units.TryGetValue(unitId, out var unit))
            return (null, Fail(404, string.Format(Vietnamese, UnitNotFoundFormat, unitId)));
        if (!unit.IsActive) return (null, Fail(400, string.Format(Vietnamese, UnitInactiveFormat, unit.Name)));
        if (unit.Product.Status == ProductStatus.Inactive)
            return (null, Fail(400, string.Format(Vietnamese, ProductInactiveFormat, unit.Product.Name)));
        // Máy bán có thể gửi kèm giá đang hiện; chỉ từ chối khi khác giá đơn vị (không cho sửa giá trên giỏ).
        if (request.UnitPrice is { } sentPrice && sentPrice != unit.SalePrice)
            return (null, Fail(400, string.Format(Vietnamese, ManualPriceFormat, lineNo)));

        return (new SaleLine
        {
            LineNo = lineNo,
            ProductId = unit.ProductId,
            UnitId = unit.Id,
            NameSnapshot = $"{unit.Product.Name} ({unit.Name})",
            Qty = request.Qty,
            UnitPrice = unit.SalePrice,
            LineTotal = UnitConverter.LineTotal(unit.SalePrice, request.Qty),
            BaseQty = UnitConverter.ToBaseQty(request.Qty, unit.Factor),
            // Lưu ý định xác nhận của người bán; sau khi trừ kho thành true chỉ khi thật sự trừ vào lô hết hạn.
            ExpiredConfirmed = request.ConfirmExpired,
        }, null);
    }

    // Trừ kho các dòng hàng đã khai báo tại vị trí bán của hóa đơn, trong transaction đang mở (NFR-06):
    // khóa sản phẩm → đọc tồn theo lô → FefoAllocator chia lô → thêm dòng sổ "sale" (trigger trừ stock).
    // Hai dòng cùng sản phẩm (lon + thùng) dùng chung bảng tồn đang trừ dở.
    private async Task<DeductOutcome> DeductStockAsync(Sale sale, int userId, bool confirmAllExpired, CancellationToken cancellationToken)
    {
        var lines = sale.SaleLines.Where(l => l.ProductId is not null).OrderBy(l => l.LineNo).ToList();
        if (lines.Count == 0) return new DeductOutcome(null, []);

        var productIds = lines.Select(l => l.ProductId!.Value).Distinct().Order().ToList();
        await unitOfWork.StockRepository.LockProductsAsync(productIds, cancellationToken);
        var stocks = await unitOfWork.StockRepository.GetLotStocksAsync(productIds, sale.LocationId, cancellationToken);
        var working = productIds.ToDictionary(id => id, id => stocks.TryGetValue(id, out var list) ? list.ToList() : []);

        var today = VietnamToday();
        var now = DateTime.UtcNow;
        var plans = new List<(SaleLine Line, AllocationResult Result)>();
        var needConfirm = new List<SaleLine>();

        foreach (var line in lines)
        {
            var productId = line.ProductId!.Value;
            var result = FefoAllocator.Allocate(working[productId], line.BaseQty!.Value, today, confirmAllExpired || line.ExpiredConfirmed);
            if (result.NeedsExpiredConfirmation) { needConfirm.Add(line); continue; }

            plans.Add((line, result));
            foreach (var allocation in result.Allocations)
                Consume(working[productId], allocation, provisionalKey: -productId, now);
        }

        if (needConfirm.Count > 0)
        {
            var detail = string.Join("; ", needConfirm.Select(l => $"{l.NameSnapshot} (dòng {l.LineNo})"));
            return new DeductOutcome(Fail(409, string.Format(Vietnamese, ExpiredFormat, detail)), []);
        }

        var warnings = new List<string>();
        var needsCount = new HashSet<int>();
        var provisionalLots = new Dictionary<int, (int? Id, Lot? NewLot)>();

        foreach (var (line, result) in plans)
        {
            var productId = line.ProductId!.Value;
            foreach (var allocation in result.Allocations)
            {
                var move = new StockMove
                {
                    ProductId = productId,
                    LocationId = sale.LocationId,
                    QtyChange = -allocation.Qty,
                    UnitCost = allocation.UnitCost,
                    DocType = DocType.Sale,
                    SaleLine = line,
                    UserId = userId,
                };

                if (allocation.LotId is { } lotId and > 0)
                {
                    move.LotId = lotId;
                }
                else
                {
                    // Lô tạm: dùng lô tạm sẵn có của sản phẩm (ở vị trí khác cũng được), chưa có thì tạo (ADR-0002).
                    if (!provisionalLots.TryGetValue(productId, out var provisional))
                    {
                        var existingId = await unitOfWork.StockRepository.GetProvisionalLotIdAsync(productId, cancellationToken);
                        Lot? newLot = null;
                        if (existingId is null)
                        {
                            newLot = new Lot { ProductId = productId, Kind = LotKind.Provisional, ReceivedAt = now };
                            await unitOfWork.StockRepository.AddLotAsync(newLot, cancellationToken);
                        }
                        provisional = (existingId, newLot);
                        provisionalLots[productId] = provisional;
                    }
                    if (provisional.Id is { } provisionalId) move.LotId = provisionalId;
                    else move.Lot = provisional.NewLot!;
                }

                await unitOfWork.StockRepository.AddAsync(move, cancellationToken);
            }

            line.CostTotal = result.CostTotal;
            line.ExpiredConfirmed = result.UsesExpired;

            if (result.UsesProvisional)
                warnings.Add(string.Format(Vietnamese, WarnProvisionalFormat, line.NameSnapshot));
            else if (result.IsShort)
                warnings.Add(string.Format(Vietnamese, WarnShortFormat, line.NameSnapshot));
            if (result.UsesExpired)
                warnings.Add(string.Format(Vietnamese, WarnExpiredFormat, line.NameSnapshot));
            if (result.IsShort || result.UsesProvisional)
                needsCount.Add(productId);
        }

        if (needsCount.Count > 0)
            await unitOfWork.StockRepository.MarkNeedsCountAsync([.. needsCount], cancellationToken);

        return new DeductOutcome(null, warnings);
    }

    // Trừ phần vừa chia khỏi bảng tồn đang làm việc. Lô tạm chưa có id dùng khóa âm (-productId) cho các dòng sau.
    private static void Consume(List<LotStock> lots, LotAllocation allocation, int provisionalKey, DateTime now)
    {
        var lotId = allocation.LotId ?? provisionalKey;
        var index = lots.FindIndex(l => l.LotId == lotId);
        if (index >= 0)
            lots[index] = lots[index] with { Qty = lots[index].Qty - allocation.Qty };
        else
            lots.Add(new LotStock(lotId, LotKind.Provisional, null, now, null, -allocation.Qty));
    }

    // Dựng phản hồi; đọc lại hóa đơn để có đủ vị trí, người bán (hóa đơn vừa thêm chưa nạp các liên kết này).
    private async Task<HTTPResponseData<SaleResponse?>> BuildResponseAsync(
        Sale saved, string? role, IReadOnlyList<string> warnings, string message, int statusCode, CancellationToken cancellationToken)
    {
        var sale = await unitOfWork.SaleRepository.GetWithLinesAsync(saved.Id, cancellationToken) ?? saved;
        var canSeeCost = role == UserRole.Owner;
        var moves = await unitOfWork.StockRepository.GetSaleMovesAsync(sale.Id, cancellationToken);
        var lotsByLine = moves
            .Where(m => m.DocType == DocType.Sale && m.SaleLineId is not null)
            .GroupBy(m => m.SaleLineId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SaleLotResponse>)[.. g.Select(m => new SaleLotResponse(
                m.LotId, m.Lot.Kind, m.Lot.LotNo, m.Lot.ExpiryDate, -m.QtyChange, canSeeCost ? m.UnitCost : null))]);

        var lines = sale.SaleLines.OrderBy(l => l.LineNo).Select(l => new SaleLineResponse(
            l.Id, l.LineNo, l.ProductId, l.UnitId, l.NameSnapshot, l.Qty, l.UnitPrice, l.LineTotal, l.BaseQty,
            canSeeCost ? l.CostTotal : null,
            l.ExpiredConfirmed,
            l.ProductId is null,
            lotsByLine.GetValueOrDefault(l.Id) ?? [])).ToList();

        // Lãi gộp chỉ tính khi đã trừ kho và biết giá vốn mọi dòng (BR-06).
        decimal? costTotal = canSeeCost && sale.PaidAt is not null && sale.SaleLines.All(l => l.CostTotal is not null)
            ? sale.SaleLines.Sum(l => l.CostTotal!.Value)
            : null;

        var response = new SaleResponse(
            sale.Id,
            sale.Code,
            sale.Status,
            sale.PaymentMethod,
            sale.LocationId,
            sale.Location.Name,
            sale.Total,
            sale.CashGiven,
            sale.CashGiven is { } given ? given - sale.Total : null,
            sale.CreatedBy,
            sale.CreatedByNavigation.Name,
            sale.CreatedAt,
            sale.PaidAt,
            sale.SyncedAt,
            sale.CancelledByNavigation?.Name,
            sale.CancelledAt,
            sale.CancelReason,
            !canSeeCost,
            costTotal,
            costTotal is { } cost ? sale.Total - cost : null,
            lines,
            warnings);
        return HTTPResponse.Success(response, message, statusCode);
    }

    private static HTTPResponseData<SaleResponse?> Fail(int statusCode, string message) =>
        HTTPResponse.Fail<SaleResponse>(statusCode, message);

    private static DateOnly VietnamToday() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone));

    // 00:00 giờ Việt Nam của ngày đó, đổi ra UTC.
    private static DateTime? StartOfDayUtc(DateOnly? date) =>
        date is { } d ? TimeZoneInfo.ConvertTimeToUtc(d.ToDateTime(TimeOnly.MinValue), VietnamTimeZone) : null;
}
