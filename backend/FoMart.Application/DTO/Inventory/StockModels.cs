using System.ComponentModel.DataAnnotations;

namespace FoMart.Application.DTO.Inventory;

// GET /api/stock: tồn theo lô và vị trí.
public sealed record StockSearchRequest
{
    public int? ProductId { get; init; }

    public int? LocationId { get; init; }

    // false = bỏ các dòng tồn bằng 0 (lô đã bán hết).
    public bool IncludeZero { get; init; }

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
