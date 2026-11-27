using FoMart.Application.Interfaces.Authentication;

namespace FoMart.Infrastructure.Authentication;

// BCrypt chuẩn ($2a$/$2b$, cost 11), tương thích hash trong database/seed.
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    // Hash giả dùng khi không có tài khoản, để Verify tốn thời gian như thật.
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), WorkFactor);

    // Băm mật khẩu hoặc PIN.
    public string Hash(string secret) => BCrypt.Net.BCrypt.HashPassword(secret, WorkFactor);

    // Kiểm tra; hash null hoặc sai định dạng thì trả false sau khi vẫn chạy một lần BCrypt.
    public bool Verify(string secret, string? hash)
    {
        if (hash is null)
        {
            BCrypt.Net.BCrypt.Verify(secret, DummyHash);
            return false;
        }
        try
        {
            return BCrypt.Net.BCrypt.Verify(secret, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
