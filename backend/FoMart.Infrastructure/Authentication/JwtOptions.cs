using System.ComponentModel.DataAnnotations;

namespace FoMart.Infrastructure.Authentication;

// Cấu hình JWT, đọc từ section "Jwt". SigningKey đặt bằng User Secrets / biến môi trường, KHÔNG ghi vào appsettings.json.
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = "";

    [Required]
    public string Audience { get; init; } = "";

    // Khóa ký HMAC-SHA256, tối thiểu 32 ký tự.
    [Required, MinLength(32)]
    public string SigningKey { get; init; } = "";

    [Range(1, 1440)]
    public int AccessTokenMinutes { get; init; } = 30;

    [Range(1, 365)]
    public int RefreshTokenDays { get; init; } = 7;
}
