namespace FoMart.Domain.Enums;

// Trạng thái sản phẩm, trùng CHECK CK_products_status trong database.
public static class ProductStatus
{
    public const string Active = "active";

    // Ngừng kinh doanh: không xóa sản phẩm, chỉ chuyển trạng thái (UC-01).
    public const string Inactive = "inactive";

    // Tạo nhanh khi quét ra mã mới, chờ chủ tiệm hoàn thiện thông tin (UC-01).
    public const string Incomplete = "incomplete";

    public static readonly IReadOnlyList<string> All = [Active, Inactive, Incomplete];
}
