using FoMart.Domain.Entities;

namespace FoMart.Application.Interfaces.Authentication;

// Access token JWT đã ký.
public sealed record IssuedAccessToken(string Token, DateTime ExpiresAt);

// Refresh token: Token trả cho client, TokenHash lưu vào auth.refresh_tokens.
public sealed record IssuedRefreshToken(string Token, string TokenHash, DateTime ExpiresAt);

// Tạo JWT và refresh token.
public interface ITokenService
{
    // Tạo access token chứa claim sub (id), name, role.
    IssuedAccessToken CreateAccessToken(User user);

    // Tạo refresh token ngẫu nhiên kèm bản băm để lưu.
    IssuedRefreshToken CreateRefreshToken();

    // Băm refresh token client gửi lên để tra trong database.
    string HashRefreshToken(string refreshToken);
}
