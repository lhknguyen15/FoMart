using FoMart.Application.DTO.Auth;
using FoMart.Domain.Entities;

namespace FoMart.Application.Interfaces.Repositories;

// Người dùng cùng bảng con: refresh_tokens.
public interface IUserRepository : IRepositoryBase<User>
{
    // Tìm người dùng theo số điện thoại (được theo dõi để sửa).
    Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);

    // Đã có chủ tiệm nào chưa (kể cả đã khóa).
    Task<bool> AnyOwnerAsync(CancellationToken cancellationToken = default);

    // Số chủ tiệm đang hoạt động (BR-15).
    Task<int> CountActiveOwnersAsync(CancellationToken cancellationToken = default);

    // Số điện thoại đã thuộc người dùng khác chưa.
    Task<bool> PhoneExistsAsync(string phone, int? excludeUserId, CancellationToken cancellationToken = default);

    // Danh sách người dùng, chủ tiệm trước.
    Task<IReadOnlyList<UserResponse>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default);

    // Tìm refresh token theo bản băm, kèm người dùng (được theo dõi để sửa).
    Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default);

    // Thu hồi mọi refresh token còn hiệu lực của người dùng (đăng xuất mọi thiết bị).
    Task RevokeActiveRefreshTokensAsync(int userId, DateTime revokedAt, CancellationToken cancellationToken = default);
}
