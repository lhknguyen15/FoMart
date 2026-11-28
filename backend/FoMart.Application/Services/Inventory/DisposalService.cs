using System.Globalization;
using System.Text;
using FoMart.Application.DTO;
using FoMart.Application.DTO.Inventory;
using FoMart.Application.Interfaces.UnitOfWork;
using FoMart.Domain.Entities;
using FoMart.Domain.Enums;
using FoMart.Domain.Rules;

namespace FoMart.Application.Services.Inventory;

// Xuất hủy (UC-08): lập phiếu → (chủ tiệm duyệt nếu nhân viên lập) → trừ tồn theo lô, ghi tổn thất theo giá vốn.
// BR-02 (đổi tồn chỉ qua sổ kho), BR-03 (phiếu đã quyết định không sửa), BR-08, BR-11, BR-15, BR-21.
public sealed class DisposalService(IUnitOfWork unitOfWork)
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly CompositeFormat InvalidReasonFormat = CompositeFormat.Parse(InventoryResponseMessageDTO.InvalidDisposalReason);
    private static readonly CompositeFormat ProductNotFoundFormat = CompositeFormat.Parse(InventoryResponseMessageDTO.DisposalProductNotFound);
    private static readonly CompositeFormat LocationNotFoundFormat = CompositeFormat.Parse(InventoryResponseMessageDTO.DisposalLocationNotFound);
    private static readonly CompositeFormat LotNotFoundFormat = CompositeFormat.Parse(InventoryResponseMessageDTO.DisposalLotNotFound);
    private static readonly CompositeFormat ShortStockFormat = CompositeFormat.Parse(InventoryResponseMessageDTO.DisposalShortStock);

    // Tìm phiếu hủy (lọc trạng thái). Tổn thất ẩn với người bị ẩn giá vốn (BR-08).
    public async Task<HTTPResponseData<PagedResult<DisposalListItemResponse>?>> SearchAsync(
        DisposalSearchRequest request, string? role, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Status) && !DisposalStatus.All.Contains(request.Status))
            return HTTPResponse.Fail<PagedResult<DisposalListItemResponse>>(400, InventoryResponseMessageDTO.InvalidDisposalStatus);

        var result = await unitOfWork.DisposalRepository.SearchAsync(request, cancellationToken);
        if (!await CanSeeCostAsync(role, cancellationToken))
            result = result with { Items = [.. result.Items.Select(i => i with { LossValue = null })] };
        return HTTPResponse.Success(result, InventoryResponseMessageDTO.SearchDisposalsSuccess);
    }

    // Lấy chi tiết phiếu hủy kèm các lô đã trừ.
    public async Task<HTTPResponseData<DisposalResponse?>> GetByIdAsync(int disposalId, string? role, CancellationToken cancellationToken)
    {
        var disposal = await unitOfWork.DisposalRepository.GetWithLinesAsync(disposalId, cancellationToken);
        if (disposal is null) return Fail(404, InventoryResponseMessageDTO.DisposalNotFound);
        return await RespondAsync(disposal, role, InventoryResponseMessageDTO.GetDisposalSuccess, 200, cancellationToken);
    }

    // Lập phiếu hủy. Chủ tiệm: duyệt và trừ tồn ngay (BR-15). Nhân viên: phiếu chờ duyệt, tồn chưa đổi (BR-21).
    public async Task<HTTPResponseData<DisposalResponse?>> CreateAsync(
        CreateDisposalRequest request, int userId, string? role, CancellationToken cancellationToken)
    {
        var validation = await ValidateLinesAsync(request.Lines, cancellationToken);
        if (validation is not null) return Fail(400, validation);

        await using var write = await unitOfWork.BeginWriteAsync(cancellationToken);
        var disposal = new Disposal
        {
            Status = DisposalStatus.Pending,
            Note = NormalizeText(request.Note),
            CreatedBy = userId,
            DisposalLines = [.. request.Lines.Select(l => new DisposalLine
            {
                ProductId = l.ProductId,
                LocationId = l.LocationId,
                LotId = l.LotId,
                BaseQty = l.BaseQty,
                Reason = l.Reason,
                Note = NormalizeText(l.Note),
            })],
        };
        await unitOfWork.DisposalRepository.AddAsync(disposal, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var isOwner = role == UserRole.Owner;
        if (isOwner)
        {
            // Nạp lại kèm sản phẩm, vị trí (cần cho thông báo thiếu hàng); cùng một entity đang được theo dõi.
            disposal = (await unitOfWork.DisposalRepository.GetWithLinesAsync(disposal.Id, cancellationToken))!;
            var shortage = await ApplyAsync(disposal, userId, cancellationToken);
            if (shortage is not null) return Fail(409, shortage); // write chưa commit → rollback, không lưu gì
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        await write.CommitAsync(cancellationToken);

        var saved = await unitOfWork.DisposalRepository.GetWithLinesAsync(disposal.Id, cancellationToken);
        return await RespondAsync(saved!, role,
            isOwner ? InventoryResponseMessageDTO.CreateDisposalApproved : InventoryResponseMessageDTO.CreateDisposalPending, 201, cancellationToken);
    }

    // Chủ tiệm duyệt phiếu chờ: trừ tồn theo lô, ghi tổn thất. Tồn không đủ thì 409 và phiếu vẫn chờ.
    public async Task<HTTPResponseData<DisposalResponse?>> ApproveAsync(int disposalId, int userId, string? role, CancellationToken cancellationToken)
    {
        await using var write = await unitOfWork.BeginWriteAsync(cancellationToken);
        var disposal = await unitOfWork.DisposalRepository.GetWithLinesAsync(disposalId, cancellationToken);
        if (disposal is null) return Fail(404, InventoryResponseMessageDTO.DisposalNotFound);
        if (disposal.Status != DisposalStatus.Pending) return Fail(409, InventoryResponseMessageDTO.DisposalNotPending);

        var shortage = await ApplyAsync(disposal, userId, cancellationToken);
        if (shortage is not null) return Fail(409, shortage);
        await unitOfWork.SaveChangesAsync(cancellationToken); // row_version chặn hai người duyệt cùng lúc → 409
        await write.CommitAsync(cancellationToken);

        var saved = await unitOfWork.DisposalRepository.GetWithLinesAsync(disposalId, cancellationToken); // nạp tên người duyệt
        return await RespondAsync(saved!, role, InventoryResponseMessageDTO.ApproveDisposalSuccess, 200, cancellationToken);
    }

    // Chủ tiệm từ chối phiếu chờ: không đổi tồn, ghi lý do (BR-21).
    public async Task<HTTPResponseData<DisposalResponse?>> RejectAsync(
        int disposalId, RejectDisposalRequest request, int userId, string? role, CancellationToken cancellationToken)
    {
        var disposal = await unitOfWork.DisposalRepository.GetWithLinesAsync(disposalId, cancellationToken);
        if (disposal is null) return Fail(404, InventoryResponseMessageDTO.DisposalNotFound);
        if (disposal.Status != DisposalStatus.Pending) return Fail(409, InventoryResponseMessageDTO.DisposalNotPending);

        disposal.Status = DisposalStatus.Rejected;
        disposal.DecidedBy = userId;
        disposal.DecidedAt = DateTime.UtcNow;
        disposal.RejectReason = request.Reason.Trim();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = await unitOfWork.DisposalRepository.GetWithLinesAsync(disposalId, cancellationToken);
        return await RespondAsync(saved!, role, InventoryResponseMessageDTO.RejectDisposalSuccess, 200, cancellationToken);
    }

    // Kiểm tra dòng trước khi lưu: lý do hợp lệ, sản phẩm và vị trí tồn tại, lô (nếu chọn) thuộc sản phẩm.
    private async Task<string?> ValidateLinesAsync(IReadOnlyList<DisposalLineRequest> lines, CancellationToken cancellationToken)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var lineNo = i + 1;
            if (!DisposalReason.All.Contains(line.Reason))
                return string.Format(Vietnamese, InvalidReasonFormat, lineNo);
            if (await unitOfWork.ProductRepository.GetByIdAsync(line.ProductId, cancellationToken) is null)
                return string.Format(Vietnamese, ProductNotFoundFormat, lineNo);
            if (await unitOfWork.LocationRepository.GetByIdAsync(line.LocationId, cancellationToken) is null)
                return string.Format(Vietnamese, LocationNotFoundFormat, lineNo);
            if (line.LotId is { } lotId)
            {
                var lots = await unitOfWork.StockRepository.SearchStockAsync(
                    new StockSearchRequest { ProductId = line.ProductId, IncludeZero = true, PageSize = 200 }, null, null, cancellationToken);
                if (!lots.Items.Any(l => l.LotId == lotId))
                    return string.Format(Vietnamese, LotNotFoundFormat, lineNo);
            }
        }
        return null;
    }

    // Trừ tồn cho mọi dòng trong transaction đang mở: khóa sản phẩm, đọc tồn theo lô (khóa dòng), chia lô, ghi sổ kho.
    // Trả về thông báo khi một dòng không đủ hàng (không ghi gì thêm); null khi đã xong.
    private async Task<string?> ApplyAsync(Disposal disposal, int userId, CancellationToken cancellationToken)
    {
        var lines = disposal.DisposalLines.OrderBy(l => l.Id).ToList();
        await unitOfWork.StockRepository.LockProductsAsync([.. lines.Select(l => l.ProductId).Distinct()], cancellationToken);

        // Tồn theo lô tại từng vị trí; trừ dần trong bộ nhớ khi nhiều dòng cùng sản phẩm / vị trí.
        var stockByLocation = new Dictionary<int, Dictionary<int, List<LotStock>>>();
        foreach (var group in lines.GroupBy(l => l.LocationId))
        {
            var lotStocks = await unitOfWork.StockRepository.GetLotStocksAsync([.. group.Select(l => l.ProductId).Distinct()], group.Key, cancellationToken);
            stockByLocation[group.Key] = lotStocks.ToDictionary(kv => kv.Key, kv => kv.Value.ToList());
        }

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var lots = stockByLocation[line.LocationId].GetValueOrDefault(line.ProductId) ?? [];
            var result = DisposalAllocator.Allocate(lots, line.LotId, line.BaseQty);
            if (result.IsShort)
            {
                var baseUnit = line.Product.ProductUnits.FirstOrDefault(u => u.IsBase)?.Name ?? "";
                return string.Format(Vietnamese, ShortStockFormat, i + 1, line.Product.Name, line.Location.Name, result.Available, baseUnit, line.BaseQty);
            }

            foreach (var allocation in result.Allocations)
            {
                await unitOfWork.StockRepository.AddAsync(new StockMove
                {
                    ProductId = line.ProductId,
                    LotId = allocation.LotId,
                    LocationId = line.LocationId,
                    QtyChange = -allocation.Qty,
                    UnitCost = allocation.UnitCost,
                    DocType = DocType.Disposal,
                    DisposalLine = line,
                    UserId = userId,
                }, cancellationToken);
                var index = lots.FindIndex(l => l.LotId == allocation.LotId);
                lots[index] = lots[index] with { Qty = lots[index].Qty - allocation.Qty };
            }
        }

        disposal.Status = DisposalStatus.Approved;
        disposal.DecidedBy = userId;
        disposal.DecidedAt = DateTime.UtcNow;
        return null;
    }

    // Dựng phản hồi: các lô đã trừ lấy từ sổ kho; giá vốn và tổn thất chỉ cho người được xem (BR-08), làm tròn về đồng (BR-11).
    private async Task<HTTPResponseData<DisposalResponse?>> RespondAsync(
        Disposal disposal, string? role, string message, int statusCode, CancellationToken cancellationToken)
    {
        var canSeeCost = await CanSeeCostAsync(role, cancellationToken);
        var moves = await unitOfWork.DisposalRepository.GetMovesAsync(disposal.Id, cancellationToken);
        var movesByLine = moves.GroupBy(m => m.DisposalLineId!.Value).ToDictionary(g => g.Key, g => g.ToList());

        long? LossOf(IReadOnlyCollection<StockMove> lineMoves) =>
            canSeeCost && lineMoves.Count > 0 && lineMoves.All(m => m.UnitCost is not null)
                ? MoneyRules.ToDong(lineMoves.Sum(m => -m.QtyChange * m.UnitCost!.Value))
                : null;

        var lines = disposal.DisposalLines.OrderBy(l => l.Id).Select(l =>
        {
            var lineMoves = movesByLine.GetValueOrDefault(l.Id) ?? [];
            return new DisposalLineResponse(
                l.Id,
                l.ProductId,
                l.Product.Name,
                l.Product.ProductUnits.FirstOrDefault(u => u.IsBase)?.Name ?? "",
                l.LocationId,
                l.Location.Name,
                l.LotId,
                l.Lot?.LotNo,
                l.Lot?.ExpiryDate,
                l.BaseQty,
                l.Reason,
                l.Note,
                LossOf(lineMoves),
                [.. lineMoves.Select(m => new DisposalLotResponse(m.LotId, m.Lot.LotNo, m.Lot.ExpiryDate, -m.QtyChange, canSeeCost ? m.UnitCost : null))]);
        }).ToList();

        var response = new DisposalResponse(
            disposal.Id,
            disposal.Code,
            disposal.Status,
            disposal.Note,
            disposal.CreatedBy,
            disposal.CreatedByNavigation.Name,
            disposal.CreatedAt,
            disposal.DecidedByNavigation?.Name,
            disposal.DecidedAt,
            disposal.RejectReason,
            !canSeeCost,
            disposal.Status == DisposalStatus.Approved ? LossOf(moves) : null,
            lines);
        return HTTPResponse.Success(response, message, statusCode);
    }

    // Chủ tiệm luôn thấy giá vốn; nhân viên thấy khi cửa hàng bật staff_can_see_cost (BR-08).
    private async Task<bool> CanSeeCostAsync(string? role, CancellationToken cancellationToken)
    {
        if (role == UserRole.Owner) return true;
        var store = await unitOfWork.StoreRepository.GetSettingsAsync(cancellationToken);
        return store?.StaffCanSeeCost == true;
    }

    private static string? NormalizeText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static HTTPResponseData<DisposalResponse?> Fail(int statusCode, string message) =>
        HTTPResponse.Fail<DisposalResponse>(statusCode, message);
}
