using FoMart.Application.DTO.Auth;
using FoMart.Application.Interfaces.Repositories;
using FoMart.Domain.Entities;
using FoMart.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FoMart.Infrastructure.Repositories;

public sealed class UserRepository(FoMartDbContext dbContext)
    : RepositoryBase<User>(dbContext), IUserRepository
{
    // Tìm theo số điện thoại (UQ_users_phone).
    public Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default) =>
        DbContext.Users.SingleOrDefaultAsync(u => u.Phone == phone, cancellationToken);

    // Đã có chủ tiệm nào chưa.
    public Task<bool> AnyOwnerAsync(CancellationToken cancellationToken = default) =>
        DbContext.Users.AnyAsync(u => u.Role == UserRole.Owner, cancellationToken);

    // Số chủ tiệm đang hoạt động.
    public Task<int> CountActiveOwnersAsync(CancellationToken cancellationToken = default) =>
        DbContext.Users.CountAsync(u => u.Role == UserRole.Owner && u.IsActive, cancellationToken);

    // Số điện thoại đã bị tài khoản khác dùng chưa.
    public Task<bool> PhoneExistsAsync(string phone, int? excludeUserId, CancellationToken cancellationToken = default) =>
        DbContext.Users.AnyAsync(u => u.Phone == phone && u.Id != excludeUserId, cancellationToken);

    // Danh sách tài khoản, chủ tiệm trước rồi theo tên.
    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default) =>
        await DbContext.Users.AsNoTracking()
            .Where(u => includeInactive || u.IsActive)
            .OrderBy(u => u.Role == UserRole.Owner ? 0 : 1).ThenBy(u => u.Name)
            .Select(u => new UserResponse(
                u.Id, u.Name, u.Phone, u.Role, u.IsActive,
                u.PasswordHash != null, u.PinHash != null, u.LastLoginAt, u.CreatedAt))
            .ToListAsync(cancellationToken);

    // Tra refresh token theo bản băm (UQ_refresh_tokens_token_hash), kèm người dùng.
    public Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        DbContext.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    // Thu hồi mọi refresh token còn hiệu lực của người dùng (chạy UPDATE thẳng, không cần SaveChanges).
    public Task RevokeActiveRefreshTokensAsync(int userId, DateTime revokedAt, CancellationToken cancellationToken = default) =>
        DbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, revokedAt), cancellationToken);
}
