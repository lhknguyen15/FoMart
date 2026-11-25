using FoMart.Application.DTO;
using FoMart.Application.DTO.Auth;
using FoMart.Application.Interfaces.Authentication;
using FoMart.Application.Interfaces.UnitOfWork;
using FoMart.Domain.Entities;
using FoMart.Domain.Enums;

namespace FoMart.Application.Services.Auth;

// Quản lý tài khoản chủ tiệm / nhân viên (UC-11, BR-15). Chỉ chủ tiệm gọi được (phân quyền ở controller).
public sealed class UserService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
{
    // Lấy danh sách tài khoản; includeInactive = true để thấy cả tài khoản đã khóa.
    public async Task<HTTPResponseData<IReadOnlyList<UserResponse>?>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        var users = await unitOfWork.UserRepository.GetAllAsync(includeInactive, cancellationToken);
        return HTTPResponse.Success(users, AuthResponseMessageDTO.GetUsersSuccess);
    }

    // Tạo tài khoản: chủ tiệm bắt buộc có mật khẩu, nhân viên bắt buộc có PIN (CK_users_credentials).
    public async Task<HTTPResponseData<UserResponse?>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (!UserRole.All.Contains(request.Role))
            return HTTPResponse.Fail<UserResponse>(400, AuthResponseMessageDTO.InvalidRole);
        if (request.Role == UserRole.Owner && request.Password is null)
            return HTTPResponse.Fail<UserResponse>(400, AuthResponseMessageDTO.OwnerNeedsPassword);
        if (request.Role == UserRole.Staff && request.Pin is null)
            return HTTPResponse.Fail<UserResponse>(400, AuthResponseMessageDTO.StaffNeedsPin);

        var phone = request.Phone.Trim();
        if (await unitOfWork.UserRepository.PhoneExistsAsync(phone, null, cancellationToken))
            return HTTPResponse.Fail<UserResponse>(409, AuthResponseMessageDTO.PhoneExists);

        var user = new User
        {
            Name = request.Name.Trim(),
            Phone = phone,
            Role = request.Role,
            PasswordHash = request.Password is null ? null : passwordHasher.Hash(request.Password),
            PinHash = request.Pin is null ? null : passwordHasher.Hash(request.Pin),
            IsActive = true,
        };
        await unitOfWork.UserRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return HTTPResponse.Success(Map(user), AuthResponseMessageDTO.CreateUserSuccess, 201);
    }

    // Đổi tên, khóa/mở khóa tài khoản. Khóa tài khoản thì thu hồi mọi phiên của tài khoản đó.
    public async Task<HTTPResponseData<UserResponse?>> UpdateAsync(int userId, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return HTTPResponse.Fail<UserResponse>(404, AuthResponseMessageDTO.UserNotFound);

        var deactivating = user.IsActive && !request.IsActive;
        if (deactivating && user.Role == UserRole.Owner
            && await unitOfWork.UserRepository.CountActiveOwnersAsync(cancellationToken) <= 1)
            return HTTPResponse.Fail<UserResponse>(409, AuthResponseMessageDTO.LastOwner);

        var now = DateTime.UtcNow;
        user.Name = request.Name.Trim();
        user.IsActive = request.IsActive;
        user.UpdatedAt = now;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (deactivating)
            await unitOfWork.UserRepository.RevokeActiveRefreshTokensAsync(userId, now, cancellationToken);

        return HTTPResponse.Success(Map(user), AuthResponseMessageDTO.UpdateUserSuccess);
    }

    // Chủ tiệm đặt lại PIN cho một tài khoản.
    public async Task<HTTPResponseData<UserResponse?>> ResetPinAsync(int userId, ResetPinRequest request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return HTTPResponse.Fail<UserResponse>(404, AuthResponseMessageDTO.UserNotFound);

        user.PinHash = passwordHasher.Hash(request.Pin);
        user.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return HTTPResponse.Success(Map(user), AuthResponseMessageDTO.ResetPinSuccess);
    }

    private static UserResponse Map(User user) =>
        new(user.Id, user.Name, user.Phone, user.Role, user.IsActive,
            user.PasswordHash is not null, user.PinHash is not null, user.LastLoginAt, user.CreatedAt);
}
