using System.ComponentModel.DataAnnotations;

namespace FoMart.Application.DTO.Sales;

// GET /api/sales: lọc danh sách hóa đơn. From/To là ngày bán theo giờ Việt Nam.
public sealed record SaleSearchRequest
{
    // Tìm theo mã hóa đơn (HD...).
    [StringLength(12)]
    public string? Keyword { get; init; }

    // pending_payment | paid | cancelled; bỏ trống = tất cả.
    public string? Status { get; init; }

    // cash | transfer; bỏ trống = tất cả.
    public string? PaymentMethod { get; init; }

    public int? CreatedBy { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

// Một dòng trong danh sách hóa đơn.
public sealed record SaleListItemResponse(
    Guid Id,
    string Code,
    string Status,
    string PaymentMethod,
    long Total,
    int LineCount,
    string CreatedByName,
    DateTime CreatedAt,
    DateTime? PaidAt);

// Chi tiết hóa đơn. CostTotal, GrossProfit = null khi người xem là nhân viên (BR-08) hoặc có phần giá vốn chưa biết (lô tạm, hàng chưa khai báo).
// Warnings chỉ có ở phản hồi lúc bán / xác nhận: tồn không đủ, dùng lô tạm, bán lô hết hạn.
public sealed record SaleResponse(
    Guid Id,
    string Code,
    string Status,
    string PaymentMethod,
    int LocationId,
    string LocationName,
    long Total,
    long? CashGiven,
    long? Change,
    int CreatedBy,
    string CreatedByName,
    DateTime CreatedAt,
    DateTime? PaidAt,
    DateTime SyncedAt,
    string? CancelledByName,
    DateTime? CancelledAt,
    string? CancelReason,
    bool CostHidden,
    decimal? CostTotal,
    decimal? GrossProfit,
    IReadOnlyList<SaleLineResponse> Lines,
    IReadOnlyList<string> Warnings);

// Một dòng hóa đơn. Name, UnitPrice là snapshot lúc bán (BR-07). IsUndeclared = hàng chưa khai báo (không trừ kho).
public sealed record SaleLineResponse(
    long Id,
    short LineNo,
    int? ProductId,
    int? UnitId,
    string Name,
    int Qty,
    long UnitPrice,
    long LineTotal,
    int? BaseQty,
    decimal? CostTotal,
    bool ExpiredConfirmed,
    bool IsUndeclared,
    IReadOnlyList<SaleLotResponse> Lots);

// Phần của một dòng đã trừ vào một lô (FEFO/FIFO, BR-04). UnitCost = null với nhân viên hoặc lô tạm.
public sealed record SaleLotResponse(int LotId, string LotKind, string? LotNo, DateOnly? ExpiryDate, int Qty, decimal? UnitCost);

// POST /api/sales: lưu hóa đơn. Id là UUID v7 do máy bán sinh; gửi lại cùng Id thì không tạo hóa đơn mới (NFR-05).
public sealed record CreateSaleRequest
{
    [Required]
    public Guid Id { get; init; }

    // cash (mặc định) | transfer.
    [Required]
    public string PaymentMethod { get; init; } = "cash";

    // Tiền khách đưa (chỉ tiền mặt, không bắt buộc) để tính tiền thối.
    [Range(0, long.MaxValue)]
    public long? CashGiven { get; init; }

    // Giờ trên máy bán (UTC); bỏ trống = giờ server.
    public DateTime? CreatedAt { get; init; }

    [Required, MinLength(1), MaxLength(200)]
    public IReadOnlyList<SaleLineRequest> Lines { get; init; } = [];
}

// Một dòng giỏ hàng. Hàng đã khai báo: gửi UnitId, giá lấy theo đơn vị (không sửa giá).
// Hàng chưa khai báo: bỏ UnitId, gửi Name và UnitPrice nhập tay (UC-05), không trừ kho.
public sealed record SaleLineRequest
{
    [Range(1, int.MaxValue)]
    public int? UnitId { get; init; }

    [Range(1, int.MaxValue)]
    public int Qty { get; init; } = 1;

    [StringLength(200)]
    public string? Name { get; init; }

    [Range(0, long.MaxValue)]
    public long? UnitPrice { get; init; }

    // true = người bán đã kiểm hàng trên tay khi chỉ còn lô hết hạn (BR-05).
    public bool ConfirmExpired { get; init; }
}

// POST /api/sales/{id}/confirm-payment: người bán bấm "Đã nhận" tiền chuyển khoản.
public sealed record ConfirmSalePaymentRequest
{
    // true = xác nhận bán lô hết hạn cho mọi dòng cần (BR-05).
    public bool ConfirmExpired { get; init; }
}

// POST /api/sales/{id}/cancel: bỏ hóa đơn chờ thanh toán, hoặc chủ tiệm hủy hóa đơn đã thanh toán (UC-06).
public sealed record CancelSaleRequest
{
    [Required, StringLength(300, MinimumLength = 1)]
    public string Reason { get; init; } = "";
}
