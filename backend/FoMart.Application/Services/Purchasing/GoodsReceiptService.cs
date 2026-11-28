using System.Globalization;
using System.Text;
using FoMart.Application.DTO;
using FoMart.Application.DTO.Purchasing;
using FoMart.Application.Interfaces.UnitOfWork;
using FoMart.Domain.Entities;
using FoMart.Domain.Enums;
using FoMart.Domain.Rules;

namespace FoMart.Application.Services.Purchasing;

// Nhập hàng (UC-04): phiếu nháp → hoàn tất (tạo lô, ghi sổ kho) → hủy bằng chứng từ đảo ngược.
// BR-02, BR-03, BR-08, BR-09, BR-11.
public sealed class GoodsReceiptService(IUnitOfWork unitOfWork)
{
    private const int MaxLines = 500;

    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    private static readonly CompositeFormat MissingPricesFormat = CompositeFormat.Parse(PurchasingResponseMessageDTO.ReceiptMissingPrices);
    private static readonly CompositeFormat ExpiryWarningFormat = CompositeFormat.Parse(PurchasingResponseMessageDTO.ExpiryWarningNeedsConfirm);
    private static readonly CompositeFormat LotsUsedFormat = CompositeFormat.Parse(PurchasingResponseMessageDTO.ReceiptLotsAlreadyUsed);
    private static readonly CompositeFormat UnitNotFoundFormat = CompositeFormat.Parse(PurchasingResponseMessageDTO.UnitNotFound);
    private static readonly CompositeFormat UnitInactiveFormat = CompositeFormat.Parse(PurchasingResponseMessageDTO.UnitInactive);
    private static readonly CompositeFormat ProductInactiveFormat = CompositeFormat.Parse(PurchasingResponseMessageDTO.ProductInactive);
    private static readonly CompositeFormat ExpiryRequiredFormat = CompositeFormat.Parse(PurchasingResponseMessageDTO.ExpiryRequired);

    // Những gì người xem được thấy: giá nhập (BR-08) và số ngày cảnh báo hạn dùng của cửa hàng.
    private readonly record struct ViewContext(bool CanSeeCost, int ExpiryWarnDays);

    // Tìm phiếu nhập có phân trang (lọc trạng thái, nhà cung cấp, mã, khoảng ngày tạo).
    public async Task<HTTPResponseData<PagedResult<GoodsReceiptListItemResponse>?>> SearchAsync(
        GoodsReceiptSearchRequest request, string? role, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Status) && !GoodsReceiptStatus.All.Contains(request.Status))
            return HTTPResponse.Fail<PagedResult<GoodsReceiptListItemResponse>>(400, PurchasingResponseMessageDTO.InvalidReceiptStatus);
        if (request.From > request.To)
            return HTTPResponse.Fail<PagedResult<GoodsReceiptListItemResponse>>(400, PurchasingResponseMessageDTO.InvalidDateRange);

        var result = await unitOfWork.GoodsReceiptRepository.SearchAsync(
            request, StartOfDayUtc(request.From), StartOfDayUtc(request.To?.AddDays(1)), cancellationToken);

        var view = await GetViewContextAsync(role, cancellationToken);
        if (!view.CanSeeCost)
            result = result with { Items = [.. result.Items.Select(i => i with { Total = null })] };
        return HTTPResponse.Success(result, PurchasingResponseMessageDTO.SearchReceiptsSuccess);
    }

    // Lấy chi tiết phiếu nhập kèm các dòng.
    public async Task<HTTPResponseData<GoodsReceiptResponse?>> GetByIdAsync(int receiptId, string? role, CancellationToken cancellationToken)
    {
        var receipt = await unitOfWork.GoodsReceiptRepository.GetWithLinesAsync(receiptId, cancellationToken);
        if (receipt is null) return Fail(404, PurchasingResponseMessageDTO.ReceiptNotFound);

        var view = await GetViewContextAsync(role, cancellationToken);
        return HTTPResponse.Success(Map(receipt, view), PurchasingResponseMessageDTO.GetReceiptSuccess);
    }

    // Gợi ý giá nhập lần trước của một đơn vị (UC-04 bước 3). Nhân viên bị ẩn giá thì không xem được (BR-08).
    public async Task<HTTPResponseData<LastPurchasePriceResponse?>> GetLastPriceAsync(int unitId, string? role, CancellationToken cancellationToken)
    {
        var view = await GetViewContextAsync(role, cancellationToken);
        if (!view.CanSeeCost)
            return HTTPResponse.Fail<LastPurchasePriceResponse>(403, PurchasingResponseMessageDTO.PriceHiddenForStaff);

        var result = await unitOfWork.GoodsReceiptRepository.GetLastPriceAsync(unitId, cancellationToken);
        return result is null
            ? HTTPResponse.Fail<LastPurchasePriceResponse>(404, PurchasingResponseMessageDTO.NoLastPrice)
            : HTTPResponse.Success(result, PurchasingResponseMessageDTO.GetLastPriceSuccess);
    }

    // Tạo phiếu nhập nháp (UC-04 bước 1), có thể kèm luôn các dòng. Chưa đổi tồn.
    public async Task<HTTPResponseData<GoodsReceiptResponse?>> CreateAsync(
        CreateGoodsReceiptRequest request, int userId, string? role, CancellationToken cancellationToken)
    {
        if (request.Lines.Count > MaxLines) return Fail(400, PurchasingResponseMessageDTO.TooManyLines);

        var view = await GetViewContextAsync(role, cancellationToken);
        if (!view.CanSeeCost && request.Lines.Any(l => l.UnitPrice is not null))
            return Fail(403, PurchasingResponseMessageDTO.PriceHiddenForStaff);

        var supplierError = await CheckSupplierAsync(request.SupplierId, cancellationToken);
        if (supplierError is not null) return supplierError;

        int locationId;
        if (request.LocationId is { } requestedLocationId)
        {
            var locationError = await CheckLocationAsync(requestedLocationId, cancellationToken);
            if (locationError is not null) return locationError;
            locationId = requestedLocationId;
        }
        else
        {
            var defaultLocation = await unitOfWork.LocationRepository.GetDefaultReceivingAsync(cancellationToken);
            if (defaultLocation is null) return Fail(409, PurchasingResponseMessageDTO.NoReceivingLocation);
            locationId = defaultLocation.Id;
        }

        var units = await unitOfWork.ProductRepository.GetUnitsWithProductAsync(
            [.. request.Lines.Select(l => l.UnitId).Distinct()], cancellationToken);

        var receipt = new GoodsReceipt
        {
            SupplierId = request.SupplierId,
            LocationId = locationId,
            Status = GoodsReceiptStatus.Draft,
            Note = NormalizeText(request.Note),
            CreatedBy = userId,
        };
        foreach (var lineRequest in request.Lines)
        {
            var unit = units.GetValueOrDefault(lineRequest.UnitId);
            var lineError = ValidateLine(lineRequest, unit);
            if (lineError is not null) return lineError;

            var line = new ReceiptLine { LineNo = (short)(receipt.ReceiptLines.Count + 1) };
            ApplyLine(line, lineRequest, unit!, lineRequest.UnitPrice);
            receipt.ReceiptLines.Add(line);
        }
        receipt.Total = SumTotal(receipt);

        await unitOfWork.GoodsReceiptRepository.AddAsync(receipt, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuildResponseAsync(receipt.Id, view, PurchasingResponseMessageDTO.CreateReceiptSuccess, 201, cancellationToken);
    }

    // Sửa thông tin chung của phiếu nháp: nhà cung cấp, vị trí nhận, ghi chú.
    public async Task<HTTPResponseData<GoodsReceiptResponse?>> UpdateAsync(
        int receiptId, UpdateGoodsReceiptRequest request, string? role, CancellationToken cancellationToken)
    {
        var receipt = await unitOfWork.GoodsReceiptRepository.GetWithLinesAsync(receiptId, cancellationToken);
        if (receipt is null) return Fail(404, PurchasingResponseMessageDTO.ReceiptNotFound);
        if (receipt.Status != GoodsReceiptStatus.Draft) return Fail(409, PurchasingResponseMessageDTO.ReceiptNotDraft);

        if (request.SupplierId != receipt.SupplierId)
        {
            var supplierError = await CheckSupplierAsync(request.SupplierId, cancellationToken);
            if (supplierError is not null) return supplierError;
        }
        if (request.LocationId != receipt.LocationId)
        {
            var locationError = await CheckLocationAsync(request.LocationId, cancellationToken);
            if (locationError is not null) return locationError;
        }

        receipt.SupplierId = request.SupplierId;
        receipt.LocationId = request.LocationId;
        receipt.Note = NormalizeText(request.Note);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var view = await GetViewContextAsync(role, cancellationToken);
        return await BuildResponseAsync(receiptId, view, PurchasingResponseMessageDTO.UpdateReceiptSuccess, 200, cancellationToken);
    }

    // Thêm một dòng vào phiếu nháp (UC-04 bước 2-4: quét mã, số lượng, giá, hạn dùng).
    public async Task<HTTPResponseData<GoodsReceiptResponse?>> AddLineAsync(
        int receiptId, ReceiptLineRequest request, string? role, CancellationToken cancellationToken)
    {
        var receipt = await unitOfWork.GoodsReceiptRepository.GetWithLinesAsync(receiptId, cancellationToken);
        if (receipt is null) return Fail(404, PurchasingResponseMessageDTO.ReceiptNotFound);
        if (receipt.Status != GoodsReceiptStatus.Draft) return Fail(409, PurchasingResponseMessageDTO.ReceiptNotDraft);
        if (receipt.ReceiptLines.Count >= MaxLines) return Fail(400, PurchasingResponseMessageDTO.TooManyLines);

        var view = await GetViewContextAsync(role, cancellationToken);
        if (!view.CanSeeCost && request.UnitPrice is not null)
            return Fail(403, PurchasingResponseMessageDTO.PriceHiddenForStaff);

        var units = await unitOfWork.ProductRepository.GetUnitsWithProductAsync([request.UnitId], cancellationToken);
        var unit = units.GetValueOrDefault(request.UnitId);
        var lineError = ValidateLine(request, unit);
        if (lineError is not null) return lineError;

        var nextLineNo = receipt.ReceiptLines.Count == 0 ? 1 : receipt.ReceiptLines.Max(l => l.LineNo) + 1;
        var line = new ReceiptLine { LineNo = (short)nextLineNo };
        ApplyLine(line, request, unit!, request.UnitPrice);
        receipt.ReceiptLines.Add(line);
        receipt.Total = SumTotal(receipt);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuildResponseAsync(receiptId, view, PurchasingResponseMessageDTO.AddLineSuccess, 201, cancellationToken);
    }

    // Sửa một dòng của phiếu nháp. Nhân viên bị ẩn giá sửa dòng thì giá chủ tiệm đã điền được giữ nguyên.
    public async Task<HTTPResponseData<GoodsReceiptResponse?>> UpdateLineAsync(
        int receiptId, int lineId, ReceiptLineRequest request, string? role, CancellationToken cancellationToken)
    {
        var receipt = await unitOfWork.GoodsReceiptRepository.GetWithLinesAsync(receiptId, cancellationToken);
        if (receipt is null) return Fail(404, PurchasingResponseMessageDTO.ReceiptNotFound);
        if (receipt.Status != GoodsReceiptStatus.Draft) return Fail(409, PurchasingResponseMessageDTO.ReceiptNotDraft);

        var line = receipt.ReceiptLines.SingleOrDefault(l => l.Id == lineId);
        if (line is null) return Fail(404, PurchasingResponseMessageDTO.LineNotFound);

        var view = await GetViewContextAsync(role, cancellationToken);
        if (!view.CanSeeCost && request.UnitPrice is not null)
            return Fail(403, PurchasingResponseMessageDTO.PriceHiddenForStaff);

        var units = await unitOfWork.ProductRepository.GetUnitsWithProductAsync([request.UnitId], cancellationToken);
        var unit = units.GetValueOrDefault(request.UnitId);
        var lineError = ValidateLine(request, unit);
        if (lineError is not null) return lineError;

        // Giữ giá cũ chỉ khi người sửa không được thấy giá và vẫn là cùng đơn vị (đổi đơn vị thì giá cũ không còn đúng).
        var unitPrice = view.CanSeeCost ? request.UnitPrice
                      : request.UnitId == line.UnitId ? line.UnitPrice
                      : null;
        ApplyLine(line, request, unit!, unitPrice);
        receipt.Total = SumTotal(receipt);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuildResponseAsync(receiptId, view, PurchasingResponseMessageDTO.UpdateLineSuccess, 200, cancellationToken);
    }

    // Xóa một dòng của phiếu nháp.
    public async Task<HTTPResponseData<GoodsReceiptResponse?>> DeleteLineAsync(
        int receiptId, int lineId, string? role, CancellationToken cancellationToken)
    {
        var receipt = await unitOfWork.GoodsReceiptRepository.GetWithLinesAsync(receiptId, cancellationToken);
        if (receipt is null) return Fail(404, PurchasingResponseMessageDTO.ReceiptNotFound);
        if (receipt.Status != GoodsReceiptStatus.Draft) return Fail(409, PurchasingResponseMessageDTO.ReceiptNotDraft);

        var line = receipt.ReceiptLines.SingleOrDefault(l => l.Id == lineId);
        if (line is null) return Fail(404, PurchasingResponseMessageDTO.LineNotFound);

        unitOfWork.GoodsReceiptRepository.RemoveLine(line);
        receipt.ReceiptLines.Remove(line);
        receipt.Total = SumTotal(receipt);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var view = await GetViewContextAsync(role, cancellationToken);
        return await BuildResponseAsync(receiptId, view, PurchasingResponseMessageDTO.DeleteLineSuccess, 200, cancellationToken);
    }

    // Xóa phiếu nháp (chưa từng đổi tồn). Phiếu đã hoàn tất không bao giờ xóa, chỉ hủy (BR-03).
    public async Task<HTTPResponseData<GoodsReceiptResponse?>> DeleteAsync(int receiptId, CancellationToken cancellationToken)
    {
        var receipt = await unitOfWork.GoodsReceiptRepository.GetWithLinesAsync(receiptId, cancellationToken);
        if (receipt is null) return Fail(404, PurchasingResponseMessageDTO.ReceiptNotFound);
        if (receipt.Status != GoodsReceiptStatus.Draft) return Fail(409, PurchasingResponseMessageDTO.ReceiptNotDraft);

        foreach (var line in receipt.ReceiptLines.ToList())
            unitOfWork.GoodsReceiptRepository.RemoveLine(line);
        unitOfWork.GoodsReceiptRepository.Delete(receipt);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return HTTPResponse.Success<GoodsReceiptResponse>(null!, PurchasingResponseMessageDTO.DeleteReceiptSuccess);
    }

    // Hoàn tất phiếu (UC-04 bước 6): mỗi dòng tạo một lô mới với giá vốn theo đơn vị cơ sở (BR-11)
    // và một dòng sổ kho "receipt"; trigger cộng vào tồn tại vị trí nhận (BR-02). Một transaction.
    public async Task<HTTPResponseData<GoodsReceiptResponse?>> CompleteAsync(
        int receiptId, CompleteGoodsReceiptRequest request, int userId, string? role, CancellationToken cancellationToken)
    {
        var view = await GetViewContextAsync(role, cancellationToken);

        await using var write = await unitOfWork.BeginWriteAsync(cancellationToken);
        var receipt = await unitOfWork.GoodsReceiptRepository.GetWithLinesAsync(receiptId, cancellationToken);
        if (receipt is null) return Fail(404, PurchasingResponseMessageDTO.ReceiptNotFound);
        if (receipt.Status != GoodsReceiptStatus.Draft) return Fail(409, PurchasingResponseMessageDTO.ReceiptNotDraft);
        if (receipt.ReceiptLines.Count == 0) return Fail(409, PurchasingResponseMessageDTO.ReceiptHasNoLines);

        var location = await unitOfWork.LocationRepository.GetByIdAsync(receipt.LocationId, cancellationToken);
        if (location is not { IsActive: true }) return Fail(409, PurchasingResponseMessageDTO.LocationInactive);

        var lines = receipt.ReceiptLines.OrderBy(l => l.LineNo).ToList();
        var missingPrices = lines.Count(l => l.UnitPrice is null);
        if (missingPrices > 0) return Fail(409, string.Format(Vietnamese, MissingPricesFormat, missingPrices));

        // Sản phẩm có thể đã được bật quản lý hạn dùng sau khi thêm dòng: kiểm lại ở đây.
        var missingExpiry = lines.FirstOrDefault(l => l.ProductUnit.Product.TrackExpiry && l.ExpiryDate is null);
        if (missingExpiry is not null)
            return Fail(400, string.Format(Vietnamese, ExpiryRequiredFormat, missingExpiry.ProductUnit.Product.Name));

        var today = VietnamToday();
        var expiryWarnings = lines
            .Select(l => (Line: l, Level: ExpiryRules.Check(EffectiveExpiry(l), today, view.ExpiryWarnDays)))
            .Where(x => x.Level != ExpiryWarningLevel.None)
            .ToList();
        if (expiryWarnings.Count > 0 && !request.ConfirmExpiryWarning)
        {
            var detail = string.Join("; ", expiryWarnings.Select(x =>
                $"{x.Line.ProductUnit.Product.Name} (dòng {x.Line.LineNo}) {(x.Level == ExpiryWarningLevel.Expired ? "đã hết hạn" : "sắp hết hạn")} {x.Line.ExpiryDate:dd/MM/yyyy}"));
            return Fail(409, string.Format(Vietnamese, ExpiryWarningFormat, detail));
        }

        var now = DateTime.UtcNow;
        foreach (var line in lines)
        {
            var factor = line.ProductUnit.Factor;
            line.BaseQty = UnitConverter.ToBaseQty(line.Qty, factor);
            var unitCost = UnitConverter.ToBaseUnitCost(line.UnitPrice!.Value, factor);

            var lot = new Lot
            {
                ProductId = line.ProductId,
                Kind = LotKind.Receipt,
                ExpiryDate = EffectiveExpiry(line),
                LotNo = line.LotNo,
                UnitCost = unitCost,
                ReceivedAt = now,
            };
            await unitOfWork.StockRepository.AddLotAsync(lot, cancellationToken);
            line.Lot = lot;

            await unitOfWork.StockRepository.AddAsync(new StockMove
            {
                ProductId = line.ProductId,
                Lot = lot,
                LocationId = receipt.LocationId,
                QtyChange = line.BaseQty,
                UnitCost = unitCost,
                DocType = DocType.Receipt,
                ReceiptLine = line,
                UserId = userId,
            }, cancellationToken);
        }

        receipt.Total = SumTotal(receipt);
        receipt.Status = GoodsReceiptStatus.Completed;
        receipt.CompletedBy = userId;
        receipt.CompletedAt = now;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);

        return await BuildResponseAsync(receiptId, view, PurchasingResponseMessageDTO.CompleteReceiptSuccess, 200, cancellationToken);
    }

    // Hủy phiếu đã hoàn tất bằng chứng từ đảo ngược "receipt_cancel" (BR-03), chỉ khi các lô của phiếu chưa bị trừ.
    // Phiếu gốc và dòng sổ cũ giữ nguyên; chỉ thêm dòng sổ âm.
    public async Task<HTTPResponseData<GoodsReceiptResponse?>> CancelAsync(
        int receiptId, CancelGoodsReceiptRequest request, int userId, string? role, CancellationToken cancellationToken)
    {
        var view = await GetViewContextAsync(role, cancellationToken);

        await using var write = await unitOfWork.BeginWriteAsync(cancellationToken);
        var receipt = await unitOfWork.GoodsReceiptRepository.GetWithLinesAsync(receiptId, cancellationToken);
        if (receipt is null) return Fail(404, PurchasingResponseMessageDTO.ReceiptNotFound);
        if (receipt.Status != GoodsReceiptStatus.Completed) return Fail(409, PurchasingResponseMessageDTO.ReceiptNotCompleted);

        // Khóa tồn của các lô trước khi kiểm, để một lượt bán đang chạy không trừ vào lô giữa chừng (NFR-06).
        var lotIds = receipt.ReceiptLines.Select(l => l.LotId!.Value).ToList();
        await unitOfWork.StockRepository.LockLotsAsync(lotIds, cancellationToken);
        var usedLots = await unitOfWork.StockRepository.GetLotsMovedByOtherDocsAsync(lotIds, DocType.Receipt, cancellationToken);
        if (usedLots.Count > 0)
            return Fail(409, string.Format(Vietnamese, LotsUsedFormat, string.Join(", ", usedLots)));

        foreach (var line in receipt.ReceiptLines)
        {
            await unitOfWork.StockRepository.AddAsync(new StockMove
            {
                ProductId = line.ProductId,
                LotId = line.LotId!.Value,
                LocationId = receipt.LocationId,
                QtyChange = -line.BaseQty,
                UnitCost = line.Lot!.UnitCost,
                DocType = DocType.ReceiptCancel,
                ReceiptLineId = line.Id,
                UserId = userId,
            }, cancellationToken);
        }

        receipt.Status = GoodsReceiptStatus.Cancelled;
        receipt.CancelledBy = userId;
        receipt.CancelledAt = DateTime.UtcNow;
        receipt.CancelReason = request.Reason.Trim();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);

        return await BuildResponseAsync(receiptId, view, PurchasingResponseMessageDTO.CancelReceiptSuccess, 200, cancellationToken);
    }

    // Đơn vị phải tồn tại, đang dùng, sản phẩm chưa ngừng kinh doanh; có quản lý hạn dùng thì bắt buộc hạn dùng.
    private static HTTPResponseData<GoodsReceiptResponse?>? ValidateLine(ReceiptLineRequest request, ProductUnit? unit)
    {
        if (unit is null) return Fail(404, string.Format(Vietnamese, UnitNotFoundFormat, request.UnitId));
        if (!unit.IsActive) return Fail(400, string.Format(Vietnamese, UnitInactiveFormat, unit.Name));
        if (unit.Product.Status == ProductStatus.Inactive)
            return Fail(400, string.Format(Vietnamese, ProductInactiveFormat, unit.Product.Name));
        if (unit.Product.TrackExpiry && request.ExpiryDate is null)
            return Fail(400, string.Format(Vietnamese, ExpiryRequiredFormat, unit.Product.Name));
        return null;
    }

    // Ghi dữ liệu một dòng; chỉ gán khóa ngoại (unit lấy AsNoTracking, gán navigation sẽ làm EF thêm mới đơn vị).
    private static void ApplyLine(ReceiptLine line, ReceiptLineRequest request, ProductUnit unit, long? unitPrice)
    {
        line.ProductId = unit.ProductId;
        line.UnitId = unit.Id;
        line.Qty = request.Qty;
        line.BaseQty = UnitConverter.ToBaseQty(request.Qty, unit.Factor);
        line.UnitPrice = unitPrice;
        line.LineTotal = unitPrice is { } price ? UnitConverter.LineTotal(price, request.Qty) : null;
        line.ExpiryDate = unit.Product.TrackExpiry ? request.ExpiryDate : null;
        line.LotNo = NormalizeText(request.LotNo);
    }

    // Hạn dùng chỉ có nghĩa với sản phẩm có quản lý hạn dùng.
    private static DateOnly? EffectiveExpiry(ReceiptLine line) =>
        line.ProductUnit.Product.TrackExpiry ? line.ExpiryDate : null;

    private static long SumTotal(GoodsReceipt receipt) => receipt.ReceiptLines.Sum(l => l.LineTotal ?? 0);

    private async Task<HTTPResponseData<GoodsReceiptResponse?>?> CheckSupplierAsync(int? supplierId, CancellationToken cancellationToken)
    {
        if (supplierId is not { } id) return null;
        var supplier = await unitOfWork.SupplierRepository.GetByIdAsync(id, cancellationToken);
        if (supplier is null) return Fail(404, PurchasingResponseMessageDTO.SupplierNotFound);
        return supplier.IsActive ? null : Fail(400, PurchasingResponseMessageDTO.SupplierInactive);
    }

    private async Task<HTTPResponseData<GoodsReceiptResponse?>?> CheckLocationAsync(int locationId, CancellationToken cancellationToken)
    {
        var location = await unitOfWork.LocationRepository.GetByIdAsync(locationId, cancellationToken);
        if (location is null) return Fail(404, PurchasingResponseMessageDTO.LocationNotFound);
        return location.IsActive ? null : Fail(400, PurchasingResponseMessageDTO.LocationInactive);
    }

    // Chủ tiệm luôn thấy giá nhập; nhân viên thấy khi cửa hàng bật staff_can_see_cost (BR-08).
    private async Task<ViewContext> GetViewContextAsync(string? role, CancellationToken cancellationToken)
    {
        var store = await unitOfWork.StoreRepository.GetSettingsAsync(cancellationToken);
        var canSeeCost = role == UserRole.Owner || store?.StaffCanSeeCost == true;
        return new ViewContext(canSeeCost, store?.ExpiryWarnDays ?? 7);
    }

    private async Task<HTTPResponseData<GoodsReceiptResponse?>> BuildResponseAsync(
        int receiptId, ViewContext view, string message, int statusCode, CancellationToken cancellationToken)
    {
        var receipt = await unitOfWork.GoodsReceiptRepository.GetWithLinesAsync(receiptId, cancellationToken);
        return HTTPResponse.Success(Map(receipt!, view), message, statusCode);
    }

    private static GoodsReceiptResponse Map(GoodsReceipt receipt, ViewContext view)
    {
        var today = VietnamToday();
        var isDraft = receipt.Status == GoodsReceiptStatus.Draft;
        var lines = receipt.ReceiptLines
            .OrderBy(l => l.LineNo)
            .Select(l =>
            {
                var factor = l.ProductUnit.Factor;
                var baseUnitCost = l.Lot?.UnitCost ?? (l.UnitPrice is { } p ? UnitConverter.ToBaseUnitCost(p, factor) : null);
                var warning = isDraft ? ExpiryRules.Check(EffectiveExpiry(l), today, view.ExpiryWarnDays) : ExpiryWarningLevel.None;
                return new ReceiptLineResponse(
                    l.Id, l.LineNo, l.ProductId, l.ProductUnit.Product.Name, l.ProductUnit.Product.TrackExpiry,
                    l.UnitId, l.ProductUnit.Name, factor, l.Qty, l.BaseQty,
                    view.CanSeeCost ? l.UnitPrice : null,
                    view.CanSeeCost ? l.LineTotal : null,
                    view.CanSeeCost ? baseUnitCost : null,
                    l.ExpiryDate, l.LotNo, l.LotId,
                    ExpiryRules.ToCode(warning));
            })
            .ToList();

        return new GoodsReceiptResponse(
            receipt.Id,
            receipt.Code,
            receipt.SupplierId,
            receipt.Supplier?.Name,
            receipt.LocationId,
            receipt.Location.Name,
            receipt.Status,
            view.CanSeeCost ? receipt.Total : null,
            receipt.Note,
            receipt.CreatedBy,
            receipt.CreatedByNavigation.Name,
            receipt.CreatedAt,
            receipt.CompletedByNavigation?.Name,
            receipt.CompletedAt,
            receipt.CancelledByNavigation?.Name,
            receipt.CancelledAt,
            receipt.CancelReason,
            !view.CanSeeCost,
            receipt.ReceiptLines.Count(l => l.UnitPrice is null),
            lines);
    }

    private static HTTPResponseData<GoodsReceiptResponse?> Fail(int statusCode, string message) =>
        HTTPResponse.Fail<GoodsReceiptResponse>(statusCode, message);

    private static string? NormalizeText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateOnly VietnamToday() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone));

    // 00:00 giờ Việt Nam của ngày đó, đổi ra UTC.
    private static DateTime? StartOfDayUtc(DateOnly? date) =>
        date is { } d ? TimeZoneInfo.ConvertTimeToUtc(d.ToDateTime(TimeOnly.MinValue), VietnamTimeZone) : null;
}
