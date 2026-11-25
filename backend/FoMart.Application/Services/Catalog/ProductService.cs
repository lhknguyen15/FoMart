using System.Globalization;
using System.Text;
using FoMart.Application.DTO;
using FoMart.Application.DTO.Catalog;
using FoMart.Application.Interfaces.UnitOfWork;
using FoMart.Domain.Entities;
using FoMart.Domain.Enums;
using FoMart.Domain.Rules;

namespace FoMart.Application.Services.Catalog;

// Sản phẩm và đơn vị (UC-01): BR-09, BR-10, BR-12, BR-20.
public sealed class ProductService(IUnitOfWork unitOfWork)
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    private static readonly CompositeFormat BarcodeAlreadyUsedFormat = CompositeFormat.Parse(CatalogResponseMessageDTO.BarcodeAlreadyUsed);
    private static readonly CompositeFormat PriceWarningFormat = CompositeFormat.Parse(CatalogResponseMessageDTO.PriceWarningNeedsConfirm);

    // Tìm sản phẩm có phân trang (SC-06).
    public async Task<HTTPResponseData<PagedResult<ProductListItemResponse>?>> SearchAsync(
        ProductSearchRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Status) && !ProductStatus.All.Contains(request.Status))
            return HTTPResponse.Fail<PagedResult<ProductListItemResponse>>(400, CatalogResponseMessageDTO.InvalidProductStatus);

        var result = await unitOfWork.ProductRepository.SearchAsync(request, cancellationToken);
        return HTTPResponse.Success(result, CatalogResponseMessageDTO.SearchProductsSuccess);
    }

    // Lấy chi tiết sản phẩm: đơn vị, tồn theo vị trí (SC-07).
    public async Task<HTTPResponseData<ProductResponse?>> GetByIdAsync(int productId, CancellationToken cancellationToken)
    {
        var response = await BuildResponseAsync(productId, cancellationToken);
        return response is null
            ? HTTPResponse.Fail<ProductResponse>(404, CatalogResponseMessageDTO.ProductNotFound)
            : HTTPResponse.Success(response, CatalogResponseMessageDTO.GetProductSuccess);
    }

    // Tra mã vạch: trả sản phẩm và đúng đơn vị mang mã đó (quét ở bán hàng, nhập hàng, kiểm kê).
    public async Task<HTTPResponseData<ProductBarcodeResponse?>> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken)
    {
        var result = await unitOfWork.ProductRepository.FindByBarcodeAsync(barcode.Trim(), cancellationToken);
        return result is null
            ? HTTPResponse.Fail<ProductBarcodeResponse>(404, CatalogResponseMessageDTO.BarcodeNotFound)
            : HTTPResponse.Success(result, CatalogResponseMessageDTO.GetProductSuccess);
    }

    // Tạo sản phẩm kèm các đơn vị và ghi lịch sử giá (UC-01 bước 1-4).
    public async Task<HTTPResponseData<ProductResponse?>> CreateAsync(
        CreateProductRequest request, int userId, CancellationToken cancellationToken)
    {
        var specs = request.Units
            .Select(u => new UnitSpec(u.Name.Trim(), u.Factor, NormalizeBarcode(u.Barcode), u.SalePrice, u.IsBase))
            .ToList();
        ProductUnitRules.EnsureValidUnitSet(specs);

        if (request.Units.Count(u => u.IsDefaultPurchase) > 1)
            return HTTPResponse.Fail<ProductResponse>(400, CatalogResponseMessageDTO.OnlyOneDefaultPurchaseUnit);

        var referenceError = await CheckReferencesAsync(request.CategoryId, request.DefaultSupplierId, cancellationToken);
        if (referenceError is not null) return referenceError;

        if (request.QuickButtonOrder is { } order
            && await unitOfWork.ProductRepository.IsQuickButtonOrderTakenAsync(order, null, cancellationToken))
            return HTTPResponse.Fail<ProductResponse>(409, CatalogResponseMessageDTO.QuickButtonTaken);

        var barcodeError = await CheckBarcodesAsync(specs.Select(s => s.Barcode), null, cancellationToken);
        if (barcodeError is not null) return barcodeError;

        var baseSalePrice = specs.Single(s => s.IsBase).SalePrice;
        var warningError = CheckPriceWarnings(ProductUnitRules.FindPriceWarnings(baseSalePrice, specs), request.ConfirmPriceWarning);
        if (warningError is not null) return warningError;

        var product = new Product
        {
            Name = request.Name.Trim(),
            CategoryId = request.CategoryId,
            DefaultSupplierId = request.DefaultSupplierId,
            TrackExpiry = request.TrackExpiry,
            MinStock = request.MinStock,
            MinShelfStock = request.MinShelfStock,
            QuickButtonOrder = request.QuickButtonOrder,
            Status = ProductStatus.Active,
            CreatedBy = userId,
        };
        foreach (var (spec, unitRequest) in specs.Zip(request.Units))
        {
            var unit = new ProductUnit
            {
                Name = spec.Name,
                Factor = spec.Factor,
                Barcode = spec.Barcode,
                SalePrice = spec.SalePrice,
                IsBase = spec.IsBase,
                IsDefaultPurchase = unitRequest.IsDefaultPurchase,
                IsActive = true,
            };
            unit.PriceHistories.Add(new PriceHistory { OldPrice = null, NewPrice = spec.SalePrice, ChangedBy = userId });
            product.ProductUnits.Add(unit);
        }

        await unitOfWork.ProductRepository.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = await BuildResponseAsync(product.Id, cancellationToken);
        return HTTPResponse.Success(response!, CatalogResponseMessageDTO.CreateProductSuccess, 201);
    }

    // Sửa thông tin chung của sản phẩm; Status = inactive để ngừng bán (UC-01).
    public async Task<HTTPResponseData<ProductResponse?>> UpdateAsync(
        int productId, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        if (!ProductStatus.All.Contains(request.Status))
            return HTTPResponse.Fail<ProductResponse>(400, CatalogResponseMessageDTO.InvalidProductStatus);

        var product = await unitOfWork.ProductRepository.GetWithUnitsAsync(productId, cancellationToken);
        if (product is null)
            return HTTPResponse.Fail<ProductResponse>(404, CatalogResponseMessageDTO.ProductNotFound);

        // Chỉ kiểm tra nhóm hàng / nhà cung cấp khi đổi sang giá trị khác (giữ nguyên thì cho qua dù đã bị ẩn).
        var referenceError = await CheckReferencesAsync(
            request.CategoryId == product.CategoryId ? null : request.CategoryId,
            request.DefaultSupplierId == product.DefaultSupplierId ? null : request.DefaultSupplierId,
            cancellationToken);
        if (referenceError is not null) return referenceError;

        if (request.QuickButtonOrder is { } order && order != product.QuickButtonOrder
            && await unitOfWork.ProductRepository.IsQuickButtonOrderTakenAsync(order, productId, cancellationToken))
            return HTTPResponse.Fail<ProductResponse>(409, CatalogResponseMessageDTO.QuickButtonTaken);

        product.Name = request.Name.Trim();
        product.CategoryId = request.CategoryId;
        product.DefaultSupplierId = request.DefaultSupplierId;
        product.TrackExpiry = request.TrackExpiry;
        product.MinStock = request.MinStock;
        product.MinShelfStock = request.MinShelfStock;
        product.QuickButtonOrder = request.QuickButtonOrder;
        product.Status = request.Status;
        product.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = await BuildResponseAsync(productId, cancellationToken);
        return HTTPResponse.Success(response!, CatalogResponseMessageDTO.UpdateProductSuccess);
    }

    // Thêm một đơn vị (không phải đơn vị cơ sở) cho sản phẩm.
    public async Task<HTTPResponseData<ProductResponse?>> AddUnitAsync(
        int productId, AddProductUnitRequest request, int userId, CancellationToken cancellationToken)
    {
        var product = await unitOfWork.ProductRepository.GetWithUnitsAsync(productId, cancellationToken);
        if (product is null)
            return HTTPResponse.Fail<ProductResponse>(404, CatalogResponseMessageDTO.ProductNotFound);

        var newSpec = new UnitSpec(request.Name.Trim(), request.Factor, NormalizeBarcode(request.Barcode), request.SalePrice, IsBase: false);
        ProductUnitRules.EnsureValidUnitSet([.. product.ProductUnits.Select(ToSpec), newSpec]);

        var barcodeError = await CheckBarcodesAsync([newSpec.Barcode], null, cancellationToken);
        if (barcodeError is not null) return barcodeError;

        var baseSalePrice = product.ProductUnits.Single(u => u.IsBase).SalePrice;
        var warningError = CheckPriceWarnings(ProductUnitRules.FindPriceWarnings(baseSalePrice, [newSpec]), request.ConfirmPriceWarning);
        if (warningError is not null) return warningError;

        var unit = new ProductUnit
        {
            Name = newSpec.Name,
            Factor = newSpec.Factor,
            Barcode = newSpec.Barcode,
            SalePrice = newSpec.SalePrice,
            IsBase = false,
            IsActive = true,
        };
        unit.PriceHistories.Add(new PriceHistory { OldPrice = null, NewPrice = newSpec.SalePrice, ChangedBy = userId });
        product.ProductUnits.Add(unit);
        product.UpdatedAt = DateTime.UtcNow;

        await SaveWithDefaultPurchaseAsync(product, unit, request.IsDefaultPurchase, cancellationToken);

        var response = await BuildResponseAsync(productId, cancellationToken);
        return HTTPResponse.Success(response!, CatalogResponseMessageDTO.AddUnitSuccess, 201);
    }

    // Sửa một đơn vị; đổi giá thì ghi lịch sử giá, đổi hệ số bị chặn khi đã có giao dịch (BR-12).
    public async Task<HTTPResponseData<ProductResponse?>> UpdateUnitAsync(
        int productId, int unitId, UpdateProductUnitRequest request, int userId, CancellationToken cancellationToken)
    {
        var product = await unitOfWork.ProductRepository.GetWithUnitsAsync(productId, cancellationToken);
        var unit = product?.ProductUnits.SingleOrDefault(u => u.Id == unitId);
        if (product is null || unit is null)
            return HTTPResponse.Fail<ProductResponse>(404, CatalogResponseMessageDTO.UnitNotFound);

        if (unit.IsBase && !request.IsActive)
            return HTTPResponse.Fail<ProductResponse>(400, CatalogResponseMessageDTO.CannotDeactivateBaseUnit);

        var updatedSpec = new UnitSpec(request.Name.Trim(), request.Factor, NormalizeBarcode(request.Barcode), request.SalePrice, unit.IsBase);
        var specs = product.ProductUnits.Select(u => u.Id == unitId ? updatedSpec : ToSpec(u)).ToList();
        ProductUnitRules.EnsureValidUnitSet(specs);

        var lockedUnitIds = await unitOfWork.ProductRepository.GetUnitIdsWithTransactionsAsync(productId, cancellationToken);
        ProductUnitRules.EnsureFactorChangeAllowed(unit.Name, unit.Factor, request.Factor, lockedUnitIds.Contains(unitId));

        var barcodeError = await CheckBarcodesAsync([updatedSpec.Barcode], unitId, cancellationToken);
        if (barcodeError is not null) return barcodeError;

        // Đổi giá đơn vị cơ sở thì kiểm lại mọi đơn vị lớn; đổi đơn vị lớn thì chỉ kiểm đơn vị đó.
        var baseSalePrice = specs.Single(s => s.IsBase).SalePrice;
        var unitsToCheck = unit.IsBase
            ? product.ProductUnits.Where(u => u.IsActive && u.Id != unitId).Select(ToSpec)
            : request.IsActive ? [updatedSpec] : [];
        var warningError = CheckPriceWarnings(ProductUnitRules.FindPriceWarnings(baseSalePrice, unitsToCheck), request.ConfirmPriceWarning);
        if (warningError is not null) return warningError;

        if (unit.SalePrice != updatedSpec.SalePrice)
            unit.PriceHistories.Add(new PriceHistory { OldPrice = unit.SalePrice, NewPrice = updatedSpec.SalePrice, ChangedBy = userId });

        unit.Name = updatedSpec.Name;
        unit.Factor = updatedSpec.Factor;
        unit.Barcode = updatedSpec.Barcode;
        unit.SalePrice = updatedSpec.SalePrice;
        unit.IsActive = request.IsActive;
        unit.UpdatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        await SaveWithDefaultPurchaseAsync(product, unit, request.IsDefaultPurchase && request.IsActive, cancellationToken);

        var response = await BuildResponseAsync(productId, cancellationToken);
        return HTTPResponse.Success(response!, CatalogResponseMessageDTO.UpdateUnitSuccess);
    }

    // Lưu thay đổi; nếu đơn vị được chọn làm đơn vị nhập mặc định thì bỏ mặc định ở đơn vị cũ trước.
    // Hai lần lưu trong một transaction vì index UX_product_units_default_purchase không cho hai đơn vị cùng mặc định.
    private async Task SaveWithDefaultPurchaseAsync(Product product, ProductUnit unit, bool makeDefault, CancellationToken cancellationToken)
    {
        if (!makeDefault || unit.IsDefaultPurchase)
        {
            if (!makeDefault) unit.IsDefaultPurchase = false;
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        await using var write = await unitOfWork.BeginWriteAsync(cancellationToken);
        foreach (var other in product.ProductUnits.Where(u => u.IsDefaultPurchase))
            other.IsDefaultPurchase = false;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        unit.IsDefaultPurchase = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await write.CommitAsync(cancellationToken);
    }

    // Dựng ProductResponse đầy đủ; null nếu không có sản phẩm.
    private async Task<ProductResponse?> BuildResponseAsync(int productId, CancellationToken cancellationToken)
    {
        var product = await unitOfWork.ProductRepository.GetWithUnitsAsync(productId, cancellationToken);
        if (product is null) return null;

        var lockedUnitIds = await unitOfWork.ProductRepository.GetUnitIdsWithTransactionsAsync(productId, cancellationToken);
        var stocks = await unitOfWork.ProductRepository.GetStockByLocationAsync(productId, cancellationToken);
        return Map(product, lockedUnitIds, stocks);
    }

    // Nhóm hàng / nhà cung cấp phải tồn tại và đang dùng; null = hợp lệ.
    private async Task<HTTPResponseData<ProductResponse?>?> CheckReferencesAsync(
        int? categoryId, int? supplierId, CancellationToken cancellationToken)
    {
        if (categoryId is { } cid)
        {
            var category = await unitOfWork.CategoryRepository.GetByIdAsync(cid, cancellationToken);
            if (category is null) return HTTPResponse.Fail<ProductResponse>(404, CatalogResponseMessageDTO.CategoryNotFound);
            if (!category.IsActive) return HTTPResponse.Fail<ProductResponse>(400, CatalogResponseMessageDTO.CategoryInactive);
        }
        if (supplierId is { } sid)
        {
            var supplier = await unitOfWork.SupplierRepository.GetByIdAsync(sid, cancellationToken);
            if (supplier is null) return HTTPResponse.Fail<ProductResponse>(404, CatalogResponseMessageDTO.SupplierNotFound);
            if (!supplier.IsActive) return HTTPResponse.Fail<ProductResponse>(400, CatalogResponseMessageDTO.SupplierInactive);
        }
        return null;
    }

    // BR-20: mã vạch không được thuộc đơn vị khác; null = hợp lệ.
    private async Task<HTTPResponseData<ProductResponse?>?> CheckBarcodesAsync(
        IEnumerable<string?> barcodes, int? excludeUnitId, CancellationToken cancellationToken)
    {
        var list = barcodes.OfType<string>().ToList();
        var used = await unitOfWork.ProductRepository.FindUsedBarcodesAsync(list, excludeUnitId, cancellationToken);
        return used.Count == 0
            ? null
            : HTTPResponse.Fail<ProductResponse>(409, string.Format(Vietnamese, BarcodeAlreadyUsedFormat, string.Join(", ", used)));
    }

    // UC-01: giá đơn vị lớn cao hơn hệ số × giá đơn vị cơ sở thì cảnh báo, chỉ lưu khi đã xác nhận; null = cho lưu.
    private static HTTPResponseData<ProductResponse?>? CheckPriceWarnings(IReadOnlyList<UnitPriceWarning> warnings, bool confirmed)
    {
        if (warnings.Count == 0 || confirmed) return null;

        var detail = string.Join("; ", warnings.Select(w =>
            $"giá {w.UnitName} {Money(w.SalePrice)} cao hơn {w.Factor} × {Money(w.BaseSalePrice)} = {Money(w.MaxReasonablePrice)}"));
        return HTTPResponse.Fail<ProductResponse>(409, string.Format(Vietnamese, PriceWarningFormat, detail));
    }

    private static string? NormalizeBarcode(string? barcode) =>
        string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();

    private static string Money(long amount) => amount.ToString("N0", Vietnamese) + "đ";

    private static UnitSpec ToSpec(ProductUnit unit) =>
        new(unit.Name, unit.Factor, unit.Barcode, unit.SalePrice, unit.IsBase);

    private static ProductResponse Map(Product product, IReadOnlySet<int> lockedUnitIds, IReadOnlyList<ProductStockResponse> stocks) =>
        new(
            product.Id,
            product.Name,
            product.CategoryId,
            product.Category?.Name,
            product.DefaultSupplierId,
            product.DefaultSupplier?.Name,
            product.TrackExpiry,
            product.MinStock,
            product.MinShelfStock,
            product.Status,
            product.NeedsCount,
            product.LastCountedAt,
            product.QuickButtonOrder,
            product.CreatedAt,
            product.UpdatedAt,
            product.ProductUnits
                .OrderByDescending(u => u.IsBase).ThenBy(u => u.Factor).ThenBy(u => u.Name)
                .Select(u => new ProductUnitResponse(
                    u.Id, u.Name, u.Factor, u.Barcode, u.SalePrice, u.IsBase, u.IsDefaultPurchase, u.IsActive,
                    lockedUnitIds.Contains(u.Id)))
                .ToList(),
            stocks);
}
