using System.ComponentModel.DataAnnotations;

namespace FoMart.Application.DTO.Purchasing;

// GET /api/goods-receipts: lọc danh sách phiếu nhập. From/To là ngày tạo theo giờ Việt Nam.
public sealed record GoodsReceiptSearchRequest
{
    // Tìm theo mã phiếu (PN...).
    [StringLength(12)]
    public string? Keyword { get; init; }

    // draft | completed | cancelled; bỏ trống = tất cả.
    public string? Status { get; init; }

    public int? SupplierId { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

// Một dòng trong danh sách phiếu nhập. Total = null khi người xem bị ẩn giá nhập (BR-08).
public sealed record GoodsReceiptListItemResponse(
    int Id,
    string Code,
    string? SupplierName,
    string LocationName,
    string Status,
    long? Total,
    int LineCount,
    string CreatedByName,
    DateTime CreatedAt,
    DateTime? CompletedAt);

// Chi tiết phiếu nhập (SC-10). PricesHidden = true thì mọi giá/tiền là null (BR-08).
public sealed record GoodsReceiptResponse(
    int Id,
    string Code,
    int? SupplierId,
    string? SupplierName,
    int LocationId,
    string LocationName,
    string Status,
    long? Total,
    string? Note,
    int CreatedBy,
    string CreatedByName,
    DateTime CreatedAt,
    string? CompletedByName,
    DateTime? CompletedAt,
    string? CancelledByName,
    DateTime? CancelledAt,
    string? CancelReason,
    bool PricesHidden,
    int MissingPriceLines,
    IReadOnlyList<ReceiptLineResponse> Lines);

// Một dòng phiếu nhập. BaseUnitCost = giá vốn mỗi đơn vị cơ sở (BR-11); LotId có sau khi hoàn tất.
// ExpiryWarning: null | near_expiry | expired (cảnh báo, phải xác nhận khi hoàn tất).
public sealed record ReceiptLineResponse(
    int Id,
    short LineNo,
    int ProductId,
    string ProductName,
    bool TrackExpiry,
    int UnitId,
    string UnitName,
    int Factor,
    int Qty,
    int BaseQty,
    long? UnitPrice,
    long? LineTotal,
    decimal? BaseUnitCost,
    DateOnly? ExpiryDate,
    string? LotNo,
    int? LotId,
    string? ExpiryWarning);

// GET /api/goods-receipts/last-price: gợi ý giá lần nhập trước của một đơn vị (UC-04 bước 3).
public sealed record LastPurchasePriceResponse(
    int UnitId,
    long UnitPrice,
    DateTime ReceivedAt,
    string ReceiptCode,
    string? SupplierName);

// POST /api/goods-receipts: tạo phiếu nháp. LocationId bỏ trống = kho sau nếu có, không thì kệ bán mặc định.
public sealed record CreateGoodsReceiptRequest
{
    public int? SupplierId { get; init; }

    public int? LocationId { get; init; }

    [StringLength(500)]
    public string? Note { get; init; }

    // Có thể gửi luôn các dòng, hoặc thêm dần bằng POST /{id}/lines.
    public IReadOnlyList<ReceiptLineRequest> Lines { get; init; } = [];
}

// PUT /api/goods-receipts/{id}: sửa thông tin chung của phiếu nháp.
public sealed record UpdateGoodsReceiptRequest
{
    public int? SupplierId { get; init; }

    [Range(1, int.MaxValue)]
    public int LocationId { get; init; }

    [StringLength(500)]
    public string? Note { get; init; }
}

// Một dòng nhập: đơn vị (quét mã thùng thì là thùng), số lượng theo đơn vị đó, giá nhập mỗi đơn vị.
// UnitPrice null: chưa có giá (nhân viên bị ẩn giá), chủ tiệm điền trước khi hoàn tất.
public sealed record ReceiptLineRequest
{
    [Range(1, int.MaxValue)]
    public int UnitId { get; init; }

    [Range(1, int.MaxValue)]
    public int Qty { get; init; }

    [Range(0, long.MaxValue)]
    public long? UnitPrice { get; init; }

    // Bắt buộc nếu sản phẩm có quản lý hạn dùng; bỏ qua nếu không.
    public DateOnly? ExpiryDate { get; init; }

    [StringLength(50)]
    public string? LotNo { get; init; }
}

// POST /api/goods-receipts/{id}/complete.
public sealed record CompleteGoodsReceiptRequest
{
    // true = đã xem và chấp nhận cảnh báo hạn dùng (đã qua hoặc còn dưới N ngày).
    public bool ConfirmExpiryWarning { get; init; }
}

// POST /api/goods-receipts/{id}/cancel: hủy phiếu đã hoàn tất bằng chứng từ đảo ngược (BR-03).
public sealed record CancelGoodsReceiptRequest
{
    [Required, StringLength(300, MinimumLength = 1)]
    public string Reason { get; init; } = "";
}
