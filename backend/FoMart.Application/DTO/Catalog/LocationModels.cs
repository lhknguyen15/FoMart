using System.ComponentModel.DataAnnotations;

namespace FoMart.Application.DTO.Catalog;

// Vị trí (UC-02). StockQty = tổng tồn tại vị trí theo đơn vị cơ sở.
public sealed record LocationResponse(int Id, string Name, string Type, bool IsDefaultSale, bool IsActive, int StockQty);

// POST /api/locations. Type: storage (kho) | shelf (kệ bán).
public sealed record CreateLocationRequest
{
    [Required, StringLength(100)]
    public string Name { get; init; } = "";

    [Required]
    public string Type { get; init; } = "";
}

// PUT /api/locations/{id}. IsDefaultSale = true để chọn làm kệ bán mặc định (bỏ mặc định ở vị trí cũ).
// IsActive = false để ngừng dùng: chỉ được khi tồn tại vị trí bằng 0 (UC-02).
public sealed record UpdateLocationRequest
{
    [Required, StringLength(100)]
    public string Name { get; init; } = "";

    [Required]
    public string Type { get; init; } = "";

    public bool IsDefaultSale { get; init; }

    public bool IsActive { get; init; } = true;
}
