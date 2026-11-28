using System.ComponentModel.DataAnnotations;

namespace FoMart.Application.DTO.Inventory;

// GET /api/disposals: lọc phiếu xuất hủy.
public sealed record DisposalSearchRequest
{
    // pending | approved | rejected; bỏ trống = tất cả.
    public string? Status { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

// Một dòng trong danh sách phiếu hủy. LossValue = tổn thất theo giá vốn (đồng), chỉ có khi đã duyệt
// và người xem được thấy giá vốn (BR-08).
public sealed record DisposalListItemResponse(
    int Id,
    string Code,
    string Status,
    int LineCount,
    int TotalBaseQty,
    long? LossValue,
    string CreatedByName,
    DateTime CreatedAt,
    string? DecidedByName,
    DateTime? DecidedAt);

// Chi tiết phiếu hủy (SC-12). LossValue null khi chưa duyệt, bị ẩn giá vốn, hoặc có phần ở lô tạm (giá vốn chưa biết).
public sealed record DisposalResponse(
    int Id,
    string Code,
    string Status,
    string? Note,
    int CreatedBy,
    string CreatedByName,
    DateTime CreatedAt,
    string? DecidedByName,
    DateTime? DecidedAt,
    string? RejectReason,
    bool CostHidden,
    long? LossValue,
    IReadOnlyList<DisposalLineResponse> Lines);

// Một dòng phiếu hủy. LotId null = để hệ thống chọn lô khi duyệt; Lots = các lô đã trừ thật (sau khi duyệt).
public sealed record DisposalLineResponse(
    int Id,
    int ProductId,
    string ProductName,
    string BaseUnitName,
    int LocationId,
    string LocationName,
    int? LotId,
    string? LotNo,
    DateOnly? ExpiryDate,
    int BaseQty,
    string Reason,
    string? Note,
    long? LossValue,
    IReadOnlyList<DisposalLotResponse> Lots);

// Phần của một dòng đã trừ vào một lô. UnitCost null với người bị ẩn giá vốn hoặc lô tạm.
public sealed record DisposalLotResponse(int LotId, string? LotNo, DateOnly? ExpiryDate, int Qty, decimal? UnitCost);

// POST /api/disposals: lập phiếu hủy. Chủ tiệm lập thì duyệt ngay (BR-15), nhân viên lập thì chờ duyệt (BR-21).
public sealed record CreateDisposalRequest
{
    [StringLength(500)]
    public string? Note { get; init; }

    [Required, MinLength(1), MaxLength(200)]
    public IReadOnlyList<DisposalLineRequest> Lines { get; init; } = [];
}

// Một dòng hủy theo đơn vị cơ sở. LotId bỏ trống: hệ thống chọn lô hạn sớm nhất tại vị trí khi duyệt.
public sealed record DisposalLineRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(1, int.MaxValue)]
    public int LocationId { get; init; }

    public int? LotId { get; init; }

    [Range(1, 1_000_000)]
    public int BaseQty { get; init; }

    // expired | broken | damaged | other
    [Required]
    public string Reason { get; init; } = "";

    [StringLength(300)]
    public string? Note { get; init; }
}

// POST /api/disposals/{id}/reject: chủ tiệm từ chối phiếu của nhân viên (BR-21); tồn giữ nguyên.
public sealed record RejectDisposalRequest
{
    [Required, StringLength(300, MinimumLength = 1)]
    public string Reason { get; init; } = "";
}
