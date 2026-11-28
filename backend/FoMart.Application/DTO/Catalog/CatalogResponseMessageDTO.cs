namespace FoMart.Application.DTO.Catalog;

public static class CatalogResponseMessageDTO
{
    // Sản phẩm
    public const string SearchProductsSuccess = "Lấy danh sách sản phẩm thành công.";
    public const string GetProductSuccess = "Lấy thông tin sản phẩm thành công.";
    public const string CreateProductSuccess = "Tạo sản phẩm thành công.";
    public const string UpdateProductSuccess = "Cập nhật sản phẩm thành công.";
    public const string ProductNotFound = "Không tìm thấy sản phẩm.";
    public const string BarcodeNotFound = "Mã vạch chưa có trong danh mục.";
    public const string InvalidProductStatus = "Trạng thái sản phẩm không hợp lệ (active, inactive, incomplete).";
    public const string InvalidStockFilter = "Bộ lọc tồn không hợp lệ (needs_count, negative, low).";
    public const string QuickButtonTaken = "Vị trí nút bấm nhanh này đã được sản phẩm khác dùng.";
    public const string BarcodeAlreadyUsed = "Mã vạch đã thuộc sản phẩm khác: {0}.";
    public const string PriceWarningNeedsConfirm = "Cảnh báo giá: {0}. Gửi lại với ConfirmPriceWarning = true nếu giá này là đúng.";

    // Đơn vị
    public const string AddUnitSuccess = "Thêm đơn vị thành công.";
    public const string UpdateUnitSuccess = "Cập nhật đơn vị thành công.";
    public const string UnitNotFound = "Không tìm thấy đơn vị của sản phẩm này.";
    public const string CannotDeactivateBaseUnit = "Không thể ngừng dùng đơn vị cơ sở.";
    public const string OnlyOneDefaultPurchaseUnit = "Chỉ được chọn một đơn vị nhập mặc định.";

    // Nhóm hàng
    public const string GetCategoriesSuccess = "Lấy danh sách nhóm hàng thành công.";
    public const string CreateCategorySuccess = "Tạo nhóm hàng thành công.";
    public const string UpdateCategorySuccess = "Cập nhật nhóm hàng thành công.";
    public const string CategoryNotFound = "Không tìm thấy nhóm hàng.";
    public const string CategoryNameExists = "Tên nhóm hàng đã tồn tại.";
    public const string CategoryInactive = "Nhóm hàng đang bị ẩn.";

    // Nhà cung cấp
    public const string SearchSuppliersSuccess = "Lấy danh sách nhà cung cấp thành công.";
    public const string GetSupplierSuccess = "Lấy thông tin nhà cung cấp thành công.";
    public const string CreateSupplierSuccess = "Tạo nhà cung cấp thành công.";
    public const string UpdateSupplierSuccess = "Cập nhật nhà cung cấp thành công.";
    public const string SupplierNotFound = "Không tìm thấy nhà cung cấp.";
    public const string SupplierInactive = "Nhà cung cấp đang bị ẩn.";

    // Vị trí
    public const string GetLocationsSuccess = "Lấy danh sách vị trí thành công.";
    public const string CreateLocationSuccess = "Tạo vị trí thành công.";
    public const string UpdateLocationSuccess = "Cập nhật vị trí thành công.";
    public const string LocationNotFound = "Không tìm thấy vị trí.";
    public const string LocationNameExists = "Tên vị trí đã tồn tại.";
    public const string InvalidLocationType = "Loại vị trí không hợp lệ (storage, shelf).";
    public const string DefaultSaleMustBeShelf = "Vị trí bán mặc định phải là kệ bán (BR-16).";
    public const string DefaultSaleCannotBeUnset = "Phải luôn có một vị trí bán mặc định: hãy chọn vị trí khác làm mặc định thay vì bỏ chọn (BR-16).";
    public const string DefaultSaleCannotBeDeactivated = "Không thể ngừng dùng vị trí bán mặc định (BR-16).";
    public const string LocationHasStock = "Chỉ ngừng dùng được vị trí khi tồn tại đó bằng 0 (UC-02).";

    // Chung
    public const string Unauthorized = "Chưa đăng nhập hoặc phiên đăng nhập không hợp lệ.";
}
