using System.ComponentModel.DataAnnotations;

namespace FoMart.Application.DTO.Auth;

// Tài khoản (UC-11). Không bao giờ trả về mật khẩu/PIN, chỉ cho biết đã đặt hay chưa.
public sealed record UserResponse(
    int Id,
    string Name,
    string Phone,
    string Role,
    bool IsActive,
    bool HasPassword,
    bool HasPin,
    DateTime? LastLoginAt,
    DateTime CreatedAt);

// POST /api/users: chủ tiệm tạo tài khoản. Chủ tiệm bắt buộc có mật khẩu; nhân viên bắt buộc có PIN.
public sealed record CreateUserRequest
{
    [Required, StringLength(100)]
    public string Name { get; init; } = "";

    [Required, RegularExpression(AuthValidation.PhonePattern, ErrorMessage = AuthValidation.PhoneMessage)]
    public string Phone { get; init; } = "";

    // owner | staff
    [Required]
    public string Role { get; init; } = "";

    [StringLength(100, MinimumLength = 8, ErrorMessage = AuthValidation.PasswordMessage)]
    public string? Password { get; init; }

    [RegularExpression(AuthValidation.PinPattern, ErrorMessage = AuthValidation.PinMessage)]
    public string? Pin { get; init; }
}

// PUT /api/users/{id}: đổi tên, khóa/mở khóa tài khoản.
public sealed record UpdateUserRequest
{
    [Required, StringLength(100)]
    public string Name { get; init; } = "";

    public bool IsActive { get; init; } = true;
}

// PUT /api/users/{id}/pin: chủ tiệm đặt lại PIN.
public sealed record ResetPinRequest
{
    [Required, RegularExpression(AuthValidation.PinPattern, ErrorMessage = AuthValidation.PinMessage)]
    public string Pin { get; init; } = "";
}
