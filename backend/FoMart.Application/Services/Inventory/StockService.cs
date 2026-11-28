using System.Globalization;
using System.Text;
using FoMart.Application.DTO;
using FoMart.Application.DTO.Inventory;
using FoMart.Application.Interfaces.UnitOfWork;
using FoMart.Domain.Enums;
using FoMart.Domain.Rules;

namespace FoMart.Application.Services.Inventory;

// Xem tồn kho: tồn theo lô và vị trí, thẻ kho, đối soát sổ kho (BR-02). Chỉ đọc.
// Ghi tồn không nằm ở đây: mỗi chứng từ (nhập, bán, chuyển, hủy, kiểm kê) tự ghi qua unitOfWork.StockRepository.
public sealed class StockService(IUnitOfWork unitOfWork)
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
    private static readonly CompositeFormat LedgerMismatchFormat = CompositeFormat.Parse(InventoryResponseMessageDTO.LedgerMismatch);

    // Tồn theo lô và vị trí; lô hết hạn sớm đứng trước. Mỗi lô có cảnh báo hạn dùng (đã hết / sắp hết, UC-08);
    // nhân viên bị ẩn giá vốn thì UnitCost = null (BR-08).
    public async Task<HTTPResponseData<PagedResult<StockLotResponse>?>> SearchAsync(
        StockSearchRequest request, string? role, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Expiry) && !StockExpiryFilter.All.Contains(request.Expiry))
            return HTTPResponse.Fail<PagedResult<StockLotResponse>>(400, InventoryResponseMessageDTO.InvalidExpiryFilter);

        var store = await unitOfWork.StoreRepository.GetSettingsAsync(cancellationToken);
        var canSeeCost = role == UserRole.Owner || store?.StaffCanSeeCost == true;
        var warnDays = store?.ExpiryWarnDays ?? 7;
        var today = VietnamToday();
        var nearBefore = today.AddDays(warnDays); // cùng mốc với ExpiryRules.Check: còn dưới warnDays ngày là sắp hết hạn
        var (expiryFrom, expiryBefore) = request.Expiry switch
        {
            StockExpiryFilter.Expired => ((DateOnly?)null, (DateOnly?)today),
            StockExpiryFilter.NearExpiry => (today, nearBefore),
            StockExpiryFilter.Warning => (null, nearBefore),
            _ => (null, null),
        };

        var result = await unitOfWork.StockRepository.SearchStockAsync(request, expiryFrom, expiryBefore, cancellationToken);
        result = result with
        {
            Items = [.. result.Items.Select(i => i with
            {
                UnitCost = canSeeCost ? i.UnitCost : null,
                ExpiryWarning = ExpiryRules.ToCode(ExpiryRules.Check(i.ExpiryDate, today, warnDays)),
            })],
        };
        return HTTPResponse.Success(result, InventoryResponseMessageDTO.SearchStockSuccess);
    }

    // Tổng hợp lô cần xử lý hạn dùng (UC-08): đã hết hạn / sắp hết hạn, số lượng và giá trị (ẩn giá trị với người bị ẩn giá vốn, BR-08).
    public async Task<HTTPResponseData<StockExpirySummaryResponse?>> GetExpirySummaryAsync(string? role, CancellationToken cancellationToken)
    {
        var store = await unitOfWork.StoreRepository.GetSettingsAsync(cancellationToken);
        var canSeeCost = role == UserRole.Owner || store?.StaffCanSeeCost == true;
        var warnDays = store?.ExpiryWarnDays ?? 7;
        var today = VietnamToday();
        var summary = await unitOfWork.StockRepository.GetExpirySummaryAsync(today, today.AddDays(warnDays), warnDays, cancellationToken);
        if (!canSeeCost)
            summary = summary with { ExpiredValue = null, NearValue = null };
        return HTTPResponse.Success(summary, InventoryResponseMessageDTO.GetExpirySummarySuccess);
    }

    // Thẻ kho của một sản phẩm: mọi biến động kèm chứng từ và tồn sau biến động.
    public async Task<HTTPResponseData<PagedResult<StockMoveResponse>?>> GetMovesAsync(
        StockMoveSearchRequest request, string? role, CancellationToken cancellationToken)
    {
        if (request.From > request.To)
            return HTTPResponse.Fail<PagedResult<StockMoveResponse>>(400, InventoryResponseMessageDTO.InvalidDateRange);

        var product = await unitOfWork.ProductRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            return HTTPResponse.Fail<PagedResult<StockMoveResponse>>(404, InventoryResponseMessageDTO.ProductNotFound);

        var result = await unitOfWork.StockRepository.GetMovesAsync(
            request, StartOfDayUtc(request.From), StartOfDayUtc(request.To?.AddDays(1)), cancellationToken);
        if (!await CanSeeCostAsync(role, cancellationToken))
            result = result with { Items = [.. result.Items.Select(i => i with { UnitCost = null })] };
        return HTTPResponse.Success(result, InventoryResponseMessageDTO.GetMovesSuccess);
    }

    // Đối soát: liệt kê các dòng tồn lệch khỏi tổng sổ kho. Rỗng nghĩa là sổ và tồn khớp nhau.
    public async Task<HTTPResponseData<IReadOnlyList<StockLedgerMismatchResponse>?>> CheckLedgerAsync(CancellationToken cancellationToken)
    {
        var mismatches = await unitOfWork.StockRepository.GetLedgerMismatchesAsync(cancellationToken);
        var message = mismatches.Count == 0
            ? InventoryResponseMessageDTO.LedgerConsistent
            : string.Format(Vietnamese, LedgerMismatchFormat, mismatches.Count);
        return HTTPResponse.Success(mismatches, message);
    }

    // Chủ tiệm luôn thấy giá vốn; nhân viên thấy khi cửa hàng bật staff_can_see_cost (BR-08).
    private async Task<bool> CanSeeCostAsync(string? role, CancellationToken cancellationToken)
    {
        if (role == UserRole.Owner) return true;
        var store = await unitOfWork.StoreRepository.GetSettingsAsync(cancellationToken);
        return store?.StaffCanSeeCost == true;
    }

    private static DateOnly VietnamToday() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone));

    // 00:00 giờ Việt Nam của ngày đó, đổi ra UTC.
    private static DateTime? StartOfDayUtc(DateOnly? date) =>
        date is { } d ? TimeZoneInfo.ConvertTimeToUtc(d.ToDateTime(TimeOnly.MinValue), VietnamTimeZone) : null;
}
