using System.ComponentModel.DataAnnotations;

namespace FoMart.Application.DTO.Catalog;

// GET /api/suppliers: tìm nhà cung cấp theo tên (không dấu) hoặc số điện thoại.
public sealed record SupplierSearchRequest
{
    [StringLength(150)]
    public string? Keyword { get; init; }

    public bool IncludeInactive { get; init; }
}

// Nhà cung cấp (UC-02).
public sealed record SupplierResponse(
    int Id,
    string Name,
    string? Phone,
    string? Zalo,
    string? Note,
    bool IsActive,
    DateTime CreatedAt);

// POST /api/suppliers
public sealed record CreateSupplierRequest
{
    [Required, StringLength(150)]
    public string Name { get; init; } = "";

    [StringLength(15), RegularExpression(@"^[0-9+]{8,15}$", ErrorMessage = "Số điện thoại chỉ gồm chữ số (8-15 ký tự).")]
    public string? Phone { get; init; }

    [StringLength(30)]
    public string? Zalo { get; init; }

    // Ví dụ: ngày giao hàng thường lệ.
    [StringLength(500)]
    public string? Note { get; init; }
}

// PUT /api/suppliers/{id}. IsActive = false để ẩn nhà cung cấp (không xóa).
public sealed record UpdateSupplierRequest
{
    [Required, StringLength(150)]
    public string Name { get; init; } = "";

    [StringLength(15), RegularExpression(@"^[0-9+]{8,15}$", ErrorMessage = "Số điện thoại chỉ gồm chữ số (8-15 ký tự).")]
    public string? Phone { get; init; }

    [StringLength(30)]
    public string? Zalo { get; init; }

    [StringLength(500)]
    public string? Note { get; init; }

    public bool IsActive { get; init; } = true;
}
