using FoMart.Application.DTO;
using FoMart.Application.DTO.Catalog;
using FoMart.Domain.Entities;

namespace FoMart.Application.Interfaces.Repositories;

// Sản phẩm cùng bảng con: product_units, price_history.
public interface IProductRepository : IRepositoryBase<Product>
{
    // Tìm sản phẩm có phân trang; keyword khớp tên (không dấu) hoặc đúng mã vạch của một đơn vị.
    Task<PagedResult<ProductListItemResponse>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken = default);

    // Lấy sản phẩm (được theo dõi để sửa) kèm đơn vị, nhóm hàng, nhà cung cấp.
    Task<Product?> GetWithUnitsAsync(int productId, CancellationToken cancellationToken = default);

    // Tồn của sản phẩm theo từng vị trí (đơn vị cơ sở).
    Task<IReadOnlyList<ProductStockResponse>> GetStockByLocationAsync(int productId, CancellationToken cancellationToken = default);

    // Tra mã vạch: trả sản phẩm và đúng đơn vị mang mã đó (đơn vị đang dùng).
    Task<ProductBarcodeResponse?> FindByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    // Trả các mã vạch trong danh sách đã thuộc đơn vị khác (bỏ qua đơn vị excludeUnitId).
    Task<IReadOnlyList<string>> FindUsedBarcodesAsync(IReadOnlyCollection<string> barcodes, int? excludeUnitId, CancellationToken cancellationToken = default);

    // Vị trí nút bấm nhanh đã có sản phẩm khác dùng chưa.
    Task<bool> IsQuickButtonOrderTakenAsync(short order, int? excludeProductId, CancellationToken cancellationToken = default);

    // Id các đơn vị của sản phẩm đã có giao dịch (bị khóa hệ số, BR-12).
    Task<IReadOnlySet<int>> GetUnitIdsWithTransactionsAsync(int productId, CancellationToken cancellationToken = default);

    // Lấy các đơn vị theo id kèm sản phẩm (nhập hàng, bán hàng): chỉ đọc.
    Task<IReadOnlyDictionary<int, ProductUnit>> GetUnitsWithProductAsync(IReadOnlyCollection<int> unitIds, CancellationToken cancellationToken = default);
}
