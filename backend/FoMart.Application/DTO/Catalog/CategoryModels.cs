using System.ComponentModel.DataAnnotations;

namespace FoMart.Application.DTO.Catalog;

// Nhóm hàng (UC-02).
public sealed record CategoryResponse(int Id, string Name, short SortOrder, bool IsActive, int ProductCount);

// POST /api/categories
public sealed record CreateCategoryRequest
{
    [Required, StringLength(100)]
    public string Name { get; init; } = "";

    [Range(0, short.MaxValue)]
    public short SortOrder { get; init; }
}

// PUT /api/categories/{id}. IsActive = false để ẩn nhóm hàng (không xóa).
public sealed record UpdateCategoryRequest
{
    [Required, StringLength(100)]
    public string Name { get; init; } = "";

    [Range(0, short.MaxValue)]
    public short SortOrder { get; init; }

    public bool IsActive { get; init; } = true;
}
