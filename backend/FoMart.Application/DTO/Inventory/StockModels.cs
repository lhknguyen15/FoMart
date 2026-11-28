using System.ComponentModel.DataAnnotations;

namespace FoMart.Application.DTO.Inventory;

// GET /api/stock: tồn theo lô và vị trí.
public sealed record StockSearchRequest
{
    public int? ProductId { get; init; }

    public int? LocationId { get; init; }

    // false = bỏ các dòng tồn bằng 0 (lô đã bán hết).
    public bool IncludeZero { get; init; }

    // Màn hình hạn dùng (UC-08): warning | expired | near_expiry (xem StockExpiryFilter). Có giá trị thì chỉ lấy
    // lô còn tồn (> 0) và lô hết hạn sớm nhất đứng đầu; mốc "sắp hết" theo số ngày cảnh báo của cửa hàng.
    public string? Expiry { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 200)]
    public int PageSize { get; init; } = 50;
}

// Tồn của một lô tại một vị trí, theo đơn vị cơ sở. UnitCost = null khi người xem bị ẩn giá vốn (BR-08) hoặc lô tạm.
// ExpiryWarning: null | near_expiry | expired theo ngày hôm nay (giờ VN) và số ngày cảnh báo của cửa hàng; service điền.
public sealed record StockLotResponse(
    int ProductId,
    string ProductName,
    string BaseUnitName,
    int LotId,
    string LotKind,
    string? LotNo,
    DateOnly? ExpiryDate,
    DateTime ReceivedAt,
    decimal? UnitCost,
    int LocationId,
    string LocationName,
    int Qty,
    string? ExpiryWarning = null);

// Giá trị của StockSearchRequest.Expiry.
public static class StockExpiryFilter
{
    public const string Warning = "warning";        // đã hết hạn + sắp hết hạn
    public const string Expired = "expired";        // hạn dùng trước hôm nay
    public const string NearExpiry = "near_expiry"; // từ hôm nay tới trước hôm nay + số ngày cảnh báo

    public static readonly IReadOnlyList<string> All = [Warning, Expired, NearExpiry];
}

// GET /api/stock/expiry-summary: tổng hợp lô cần xử lý hạn dùng (UC-08), chỉ tính lô còn tồn.
// Value = số lượng × giá vốn lô, làm tròn về đồng; null khi người xem bị ẩn giá vốn (BR-08). Lô tạm không có hạn dùng nên không tính.
public sealed record StockExpirySummaryResponse(
    int WarnDays,
    int ExpiredLots,
    int ExpiredQty,
    long? ExpiredValue,
    int NearLots,
    int NearQty,
    long? NearValue);

// GET /api/stock/moves: thẻ kho của một sản phẩm. From/To là ngày theo giờ Việt Nam.
public sealed record StockMoveSearchRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    public int? LocationId { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 200)]
    public int PageSize { get; init; } = 50;
}

// Một dòng thẻ kho (mới nhất trước). Balance = tồn sau biến động này (theo vị trí nếu có lọc vị trí).
// SaleId / ReceiptId: chứng từ gốc để mở chi tiết (hóa đơn, phiếu nhập); các chứng từ khác chưa có màn hình nên null.
public sealed record StockMoveResponse(
    long Id,
    DateTime CreatedAt,
    string DocType,
    string DocCode,
    int LotId,
    string? LotNo,
    DateOnly? ExpiryDate,
    int LocationId,
    string LocationName,
    int QtyChange,
    int Balance,
    decimal? UnitCost,
    string UserName,
    Guid? SaleId,
    int? ReceiptId);

// GET /api/stock/ledger-check: một cặp (lô, vị trí) có tồn lệch khỏi tổng sổ kho. Danh sách phải luôn rỗng (BR-02).
public sealed record StockLedgerMismatchResponse(int? LotId, int? LocationId, int? StockQty, int? LedgerQty);
