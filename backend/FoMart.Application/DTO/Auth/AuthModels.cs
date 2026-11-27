using System.ComponentModel.DataAnnotations;

namespace FoMart.Application.DTO.Auth;

// POST /api/auth/setup: tạo chủ tiệm đầu tiên, chỉ dùng được khi chưa có chủ tiệm nào (BR-15).
public sealed record SetupOwnerRequest
{
    [Required, StringLength(100)]
    public string Name { get; init; } = "";

    [Required, RegularExpression(AuthValidation.PhonePattern, ErrorMessage = AuthValidation.PhoneMessage)]
    public string Phone { get; init; } = "";

    [Required, StringLength(100, MinimumLength = 8, ErrorMessage = AuthValidation.PasswordMessage)]
    public string Password { get; init; } = "";

    // PIN để đổi ca nhanh; không bắt buộc với chủ tiệm.
    [RegularExpression(AuthValidation.PinPattern, ErrorMessage = AuthValidation.PinMessage)]
    public string? Pin { get; init; }
}

// POST /api/auth/login: đăng nhập bằng số điện thoại + mật khẩu (SC-01).
public sealed record LoginRequest
{
    [Required]
    public string Phone { get; init; } = "";

    [Required]
    public string Password { get; init; } = "";
}

// POST /api/auth/login-pin: đổi ca nhanh bằng số điện thoại + PIN 4-6 số (UC-11, SC-01).
public sealed record PinLoginRequest
{
    [Required]
    public string Phone { get; init; } = "";

    [Required, RegularExpression(AuthValidation.PinPattern, ErrorMessage = AuthValidation.PinMessage)]
    public string Pin { get; init; } = "";
}

// POST /api/auth/refresh và /api/auth/logout.
public sealed record RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = "";
}

// PUT /api/auth/password: đổi mật khẩu của chính mình.
public sealed record ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; init; } = "";

    [Required, StringLength(100, MinimumLength = 8, ErrorMessage = AuthValidation.PasswordMessage)]
    public string NewPassword { get; init; } = "";
}

// Kết quả đăng nhập / làm mới token. Client gửi AccessToken trong header Authorization: Bearer ...
public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    CurrentUserResponse User);

// Người dùng đang đăng nhập.
public sealed record CurrentUserResponse(int Id, string Name, string Phone, string Role);

// Quy tắc kiểm tra dùng chung cho các request của mảng Auth.
public static class AuthValidation
{
    public const string PhonePattern = @"^[0-9+]{8,15}$";
    public const string PhoneMessage = "Số điện thoại chỉ gồm chữ số (8-15 ký tự).";
    public const string PinPattern = @"^[0-9]{4,6}$";
    public const string PinMessage = "PIN gồm 4-6 chữ số.";
    public const string PasswordMessage = "Mật khẩu dài 8-100 ký tự.";
}
