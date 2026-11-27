namespace FoMart.Application.DTO.Auth;

public static class AuthResponseMessageDTO
{
    // Đăng nhập
    public const string LoginSuccess = "Đăng nhập thành công.";
    public const string InvalidCredentials = "Số điện thoại hoặc mật khẩu không đúng.";
    public const string InvalidPinCredentials = "Số điện thoại hoặc PIN không đúng.";
    public const string RefreshSuccess = "Làm mới phiên đăng nhập thành công.";
    public const string InvalidRefreshToken = "Phiên đăng nhập đã hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại.";
    public const string LogoutSuccess = "Đăng xuất thành công.";
    public const string GetMeSuccess = "Lấy thông tin tài khoản thành công.";
    public const string Unauthorized = "Chưa đăng nhập hoặc phiên đăng nhập không hợp lệ.";
    public const string Forbidden = "Bạn không có quyền thực hiện thao tác này.";
    public const string TooManyRequests = "Thử quá nhiều lần. Vui lòng đợi một phút rồi thử lại.";

    // Thiết lập lần đầu
    public const string SetupSuccess = "Đã tạo tài khoản chủ tiệm.";
    public const string SetupAlreadyDone = "Cửa hàng đã có chủ tiệm. Hãy đăng nhập.";

    // Mật khẩu / PIN
    public const string ChangePasswordSuccess = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại trên các thiết bị khác.";
    public const string WrongCurrentPassword = "Mật khẩu hiện tại không đúng.";
    public const string NoPasswordSet = "Tài khoản này đăng nhập bằng PIN, chưa có mật khẩu.";
    public const string ResetPinSuccess = "Đặt lại PIN thành công.";

    // Quản lý tài khoản
    public const string GetUsersSuccess = "Lấy danh sách tài khoản thành công.";
    public const string CreateUserSuccess = "Tạo tài khoản thành công.";
    public const string UpdateUserSuccess = "Cập nhật tài khoản thành công.";
    public const string UserNotFound = "Không tìm thấy tài khoản.";
    public const string PhoneExists = "Số điện thoại đã được dùng cho tài khoản khác.";
    public const string InvalidRole = "Vai trò không hợp lệ (owner, staff).";
    public const string OwnerNeedsPassword = "Tài khoản chủ tiệm bắt buộc có mật khẩu.";
    public const string StaffNeedsPin = "Tài khoản nhân viên bắt buộc có PIN.";
    public const string LastOwner = "Phải luôn có ít nhất một chủ tiệm đang hoạt động (BR-15).";
}
