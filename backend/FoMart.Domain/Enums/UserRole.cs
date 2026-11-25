namespace FoMart.Domain.Enums;

// Vai trò người dùng, trùng CHECK CK_users_role trong database và claim "role" trong JWT.
public static class UserRole
{
    // Chủ tiệm: toàn quyền (spec mục 3). Luôn có ít nhất một chủ tiệm đang hoạt động (BR-15).
    public const string Owner = "owner";

    // Nhân viên: bán hàng, nhập hàng, kiểm kê; không xem lãi/giá vốn (BR-08).
    public const string Staff = "staff";

    public static readonly IReadOnlyList<string> All = [Owner, Staff];
}
