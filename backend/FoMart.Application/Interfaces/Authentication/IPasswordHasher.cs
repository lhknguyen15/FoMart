namespace FoMart.Application.Interfaces.Authentication;

// Băm và kiểm tra mật khẩu / PIN (NFR-09). Không bao giờ lưu bản gốc.
public interface IPasswordHasher
{
    // Băm mật khẩu hoặc PIN.
    string Hash(string secret);

    // Kiểm tra mật khẩu/PIN với bản băm; hash null thì vẫn tốn thời gian như thật để không lộ tài khoản có tồn tại.
    bool Verify(string secret, string? hash);
}
