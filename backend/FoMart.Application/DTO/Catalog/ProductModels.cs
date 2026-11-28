using System.ComponentModel.DataAnnotations;

namespace FoMart.Application.DTO.Catalog;

// GET /api/products: lọc danh sách sản phẩm (SC-06). Keyword tìm không dấu theo tên hoặc đúng mã vạch.
public sealed record ProductSearchRequest
{
    [StringLength(200)]
    public string? Keyword { get; init; }

    public int? CategoryId { get; init; }

    // active | inactive | incomplete; bỏ trống = tất cả.
    public string? Status { get; init; }

    // Lọc theo tồn (màn hình Tồn kho): needs_count | negative | low (xem ProductStockFilter); bỏ trống = tất cả.
    public string? StockFilter { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

// Giá trị của ProductSearchRequest.StockFilter.
public static class ProductStockFilter
{
    public const string NeedsCount = "needs_count"; // đánh dấu cần kiểm kê (BR-01)
    public const string Negative = "negative";      // tổng tồn âm
    public const string Low = "low";                // dưới mức tồn tối thiểu (min_stock > 0), kể cả tồn âm

    public static readonly IReadOnlyList<string> All = [NeedsCount, Negative, Low];
}

// Một dòng trong danh sách sản phẩm (SC-06).
public sealed record ProductListItemResponse(
    int Id,
    string Name,
    string? CategoryName,
    string Status,
    string BaseUnitName,
    long BaseSalePrice,
    string? BaseBarcode,
    int TotalStock,
    bool NeedsCount,
    int MinStock);

// Chi tiết sản phẩm (SC-07): thông tin chung, bảng đơn vị, tồn theo vị trí.
public sealed record ProductResponse(
    int Id,
    string Name,
    int? CategoryId,
    string? CategoryName,
    int? DefaultSupplierId,
    string? DefaultSupplierName,
    bool TrackExpiry,
    int MinStock,
    int MinShelfStock,
    string Status,
    bool NeedsCount,
    DateTime? LastCountedAt,
    short? QuickButtonOrder,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<ProductUnitResponse> Units,
    IReadOnlyList<ProductStockResponse> Stocks);

// Một đơn vị của sản phẩm. HasTransactions = true thì hệ số bị khóa (BR-12).
public sealed record ProductUnitResponse(
    int Id,
    string Name,
    int Factor,
    string? Barcode,
    long SalePrice,
    bool IsBase,
    bool IsDefaultPurchase,
    bool IsActive,
    bool HasTransactions);

// Tồn của sản phẩm tại một vị trí, theo đơn vị cơ sở.
public sealed record ProductStockResponse(int LocationId, string LocationName, int Qty);

// GET /api/products/barcode/{barcode}: kết quả quét mã (bán hàng, nhập hàng, kiểm kê).
public sealed record ProductBarcodeResponse(
    int ProductId,
    string ProductName,
    string ProductStatus,
    bool TrackExpiry,
    int UnitId,
    string UnitName,
    int Factor,
    long SalePrice);

// POST /api/products: tạo sản phẩm kèm các đơn vị (UC-01).
public sealed record CreateProductRequest
{
    [Required, StringLength(200)]
    public string Name { get; init; } = "";

    public int? CategoryId { get; init; }

    public int? DefaultSupplierId { get; init; }

    public bool TrackExpiry { get; init; }

    [Range(0, int.MaxValue)]
    public int MinStock { get; init; }

    [Range(0, int.MaxValue)]
    public int MinShelfStock { get; init; }

    [Range(1, short.MaxValue)]
    public short? QuickButtonOrder { get; init; }

    [Required, MinLength(1)]
    public IReadOnlyList<CreateProductUnitRequest> Units { get; init; } = [];

    // true = đã xem và chấp nhận cảnh báo giá đơn vị lớn.
    public bool ConfirmPriceWarning { get; init; }
}

// Một đơn vị khi tạo sản phẩm.
public sealed record CreateProductUnitRequest
{
    [Required, StringLength(30)]
    public string Name { get; init; } = "";

    [Range(1, int.MaxValue)]
    public int Factor { get; init; } = 1;

    [StringLength(32)]
    public string? Barcode { get; init; }

    [Range(0, long.MaxValue)]
    public long SalePrice { get; init; }

    public bool IsBase { get; init; }

    public bool IsDefaultPurchase { get; init; }
}

// PUT /api/products/{id}: sửa thông tin chung. Status = inactive để ngừng bán.
public sealed record UpdateProductRequest
{
    [Required, StringLength(200)]
    public string Name { get; init; } = "";

    public int? CategoryId { get; init; }

    public int? DefaultSupplierId { get; init; }

    public bool TrackExpiry { get; init; }

    [Range(0, int.MaxValue)]
    public int MinStock { get; init; }

    [Range(0, int.MaxValue)]
    public int MinShelfStock { get; init; }

    [Range(1, short.MaxValue)]
    public short? QuickButtonOrder { get; init; }

    [Required]
    public string Status { get; init; } = "";
}

// POST /api/products/{id}/units: thêm đơn vị (không phải đơn vị cơ sở).
public sealed record AddProductUnitRequest
{
    [Required, StringLength(30)]
    public string Name { get; init; } = "";

    [Range(1, int.MaxValue)]
    public int Factor { get; init; } = 1;

    [StringLength(32)]
    public string? Barcode { get; init; }

    [Range(0, long.MaxValue)]
    public long SalePrice { get; init; }

    public bool IsDefaultPurchase { get; init; }

    public bool ConfirmPriceWarning { get; init; }
}

// PUT /api/products/{id}/units/{unitId}: sửa đơn vị. Đổi Factor bị chặn khi đã có giao dịch (BR-12).
public sealed record UpdateProductUnitRequest
{
    [Required, StringLength(30)]
    public string Name { get; init; } = "";

    [Range(1, int.MaxValue)]
    public int Factor { get; init; } = 1;

    [StringLength(32)]
    public string? Barcode { get; init; }

    [Range(0, long.MaxValue)]
    public long SalePrice { get; init; }

    public bool IsDefaultPurchase { get; init; }

    public bool IsActive { get; init; } = true;

    public bool ConfirmPriceWarning { get; init; }
}
