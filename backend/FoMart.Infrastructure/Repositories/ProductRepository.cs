using FoMart.Application.DTO;
using FoMart.Application.DTO.Catalog;
using FoMart.Application.Interfaces.Repositories;
using FoMart.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoMart.Infrastructure.Repositories;

public sealed class ProductRepository(FoMartDbContext dbContext)
    : RepositoryBase<Product>(dbContext), IProductRepository
{
    // Tìm sản phẩm có phân trang; keyword khớp tên (collation bỏ dấu) hoặc đúng mã vạch.
    public async Task<PagedResult<ProductListItemResponse>> SearchAsync(
        ProductSearchRequest request, CancellationToken cancellationToken = default)
    {
        var query = DbContext.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(p => p.Name.Contains(keyword) || p.ProductUnits.Any(u => u.Barcode == keyword));
        }
        if (request.CategoryId is { } categoryId)
            query = query.Where(p => p.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(p => p.Status == request.Status);
        query = request.StockFilter switch
        {
            ProductStockFilter.NeedsCount => query.Where(p => p.NeedsCount),
            ProductStockFilter.Negative => query.Where(p => (DbContext.Stocks.Where(s => s.ProductId == p.Id).Sum(s => (int?)s.Qty) ?? 0) < 0),
            ProductStockFilter.Low => query.Where(p => p.MinStock > 0 && (DbContext.Stocks.Where(s => s.ProductId == p.Id).Sum(s => (int?)s.Qty) ?? 0) < p.MinStock),
            _ => query,
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Name).ThenBy(p => p.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductListItemResponse(
                p.Id,
                p.Name,
                p.Category != null ? p.Category.Name : null,
                p.Status,
                p.ProductUnits.Where(u => u.IsBase).Select(u => u.Name).FirstOrDefault() ?? "",
                p.ProductUnits.Where(u => u.IsBase).Select(u => u.SalePrice).FirstOrDefault(),
                p.ProductUnits.Where(u => u.IsBase).Select(u => u.Barcode).FirstOrDefault(),
                DbContext.Stocks.Where(s => s.ProductId == p.Id).Sum(s => (int?)s.Qty) ?? 0,
                p.NeedsCount,
                p.MinStock))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductListItemResponse>(items, request.Page, request.PageSize, total);
    }

    // Lấy sản phẩm được theo dõi kèm đơn vị, nhóm hàng, nhà cung cấp.
    public Task<Product?> GetWithUnitsAsync(int productId, CancellationToken cancellationToken = default) =>
        DbContext.Products
            .Include(p => p.ProductUnits)
            .Include(p => p.Category)
            .Include(p => p.DefaultSupplier)
            .SingleOrDefaultAsync(p => p.Id == productId, cancellationToken);

    // Tồn theo vị trí, chỉ các vị trí có dòng tồn.
    public async Task<IReadOnlyList<ProductStockResponse>> GetStockByLocationAsync(
        int productId, CancellationToken cancellationToken = default) =>
        await DbContext.Stocks.AsNoTracking()
            .Where(s => s.ProductId == productId)
            .GroupBy(s => new { s.LocationId, LocationName = s.Location.Name })
            .OrderBy(g => g.Key.LocationName)
            .Select(g => new ProductStockResponse(g.Key.LocationId, g.Key.LocationName, g.Sum(s => s.Qty)))
            .ToListAsync(cancellationToken);

    // Tra mã vạch của đơn vị đang dùng.
    public Task<ProductBarcodeResponse?> FindByBarcodeAsync(string barcode, CancellationToken cancellationToken = default) =>
        DbContext.ProductUnits.AsNoTracking()
            .Where(u => u.Barcode == barcode && u.IsActive)
            .Select(u => new ProductBarcodeResponse(
                u.ProductId, u.Product.Name, u.Product.Status, u.Product.TrackExpiry,
                u.Id, u.Name, u.Factor, u.SalePrice))
            .SingleOrDefaultAsync(cancellationToken);

    // Mã vạch đã thuộc đơn vị khác (kể cả đơn vị đã ngừng dùng, vì index unique vẫn giữ mã đó).
    public async Task<IReadOnlyList<string>> FindUsedBarcodesAsync(
        IReadOnlyCollection<string> barcodes, int? excludeUnitId, CancellationToken cancellationToken = default)
    {
        if (barcodes.Count == 0) return [];
        return await DbContext.ProductUnits.AsNoTracking()
            .Where(u => u.Barcode != null && barcodes.Contains(u.Barcode) && u.Id != excludeUnitId)
            .Select(u => u.Barcode!)
            .ToListAsync(cancellationToken);
    }

    // Vị trí nút bấm nhanh đã bị sản phẩm khác dùng chưa.
    public Task<bool> IsQuickButtonOrderTakenAsync(short order, int? excludeProductId, CancellationToken cancellationToken = default) =>
        DbContext.Products.AnyAsync(p => p.QuickButtonOrder == order && p.Id != excludeProductId, cancellationToken);

    // Giống trigger TR_product_units_lock_factor: sản phẩm có sổ kho thì mọi đơn vị bị khóa;
    // ngoài ra đơn vị đã nằm trên dòng bán, nhập, đặt hàng cũng bị khóa.
    public async Task<IReadOnlySet<int>> GetUnitIdsWithTransactionsAsync(int productId, CancellationToken cancellationToken = default)
    {
        var units = DbContext.ProductUnits.AsNoTracking().Where(u => u.ProductId == productId);

        var productHasMoves = await DbContext.StockMoves.AnyAsync(m => m.ProductId == productId, cancellationToken);
        var ids = productHasMoves
            ? await units.Select(u => u.Id).ToListAsync(cancellationToken)
            : await units.Where(u => u.SaleLines.Any() || u.ReceiptLines.Any() || u.PurchaseOrderLines.Any())
                         .Select(u => u.Id).ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    // Đơn vị theo id kèm sản phẩm, chỉ đọc (AsNoTracking): service chỉ gán khóa ngoại, không gán navigation.
    public async Task<IReadOnlyDictionary<int, ProductUnit>> GetUnitsWithProductAsync(
        IReadOnlyCollection<int> unitIds, CancellationToken cancellationToken = default)
    {
        if (unitIds.Count == 0) return new Dictionary<int, ProductUnit>();
        return await DbContext.ProductUnits.AsNoTracking()
            .Include(u => u.Product)
            .Where(u => unitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);
    }
}
