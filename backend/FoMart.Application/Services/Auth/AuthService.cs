using FoMart.Application.DTO;
using FoMart.Application.DTO.Auth;
using FoMart.Application.Interfaces.Authentication;
using FoMart.Application.Interfaces.UnitOfWork;
using FoMart.Domain.Entities;
using FoMart.Domain.Enums;

namespace FoMart.Application.Services.Auth;

// Đăng nhập, làm mới phiên, đổi mật khẩu (UC-11, SC-01, NFR-09).
public sealed class AuthService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ITokenService tokenService)
{
    // Tạo chủ tiệm đầu tiên và đăng nhập luôn; chỉ chạy được khi chưa có chủ tiệm nào (BR-15).
    public async Task<HTTPResponseData<AuthResponse?>> SetupOwnerAsync(SetupOwnerRequest request, CancellationToken cancellationToken)
    {
        if (await unitOfWork.UserRepository.AnyOwnerAsync(cancellationToken))
            return HTTPResponse.Fail<AuthResponse>(409, AuthResponseMessageDTO.SetupAlreadyDone);

        var phone = request.Phone.Trim();
        if (await unitOfWork.UserRepository.PhoneExistsAsync(phone, null, cancellationToken))
            return HTTPResponse.Fail<AuthResponse>(409, AuthResponseMessageDTO.PhoneExists);

        var owner = new User
        {
            Name = request.Name.Trim(),
            Phone = phone,
            Role = UserRole.Owner,
            PasswordHash = passwordHasher.Hash(request.Password),
            PinHash = request.Pin is null ? null : passwordHasher.Hash(request.Pin),
            IsActive = true,
        };
        await unitOfWork.UserRepository.AddAsync(owner, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken); // lưu trước để có Id cho claim "sub" của token

        var response = await IssueTokensAsync(owner, isLogin: true, cancellationToken);
        return HTTPResponse.Success(response, AuthResponseMessageDTO.SetupSuccess, 201);
    }

    // Đăng nhập bằng số điện thoại + mật khẩu.
    public async Task<HTTPResponseData<AuthResponse?>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByPhoneAsync(request.Phone.Trim(), cancellationToken);

        // Luôn chạy Verify (kể cả khi không có user) để thời gian phản hồi không lộ số điện thoại có tồn tại.
        var valid = passwordHasher.Verify(request.Password, user?.PasswordHash);
        if (user is null || !user.IsActive || !valid)
            return HTTPResponse.Fail<AuthResponse>(401, AuthResponseMessageDTO.InvalidCredentials);

        var response = await IssueTokensAsync(user, isLogin: true, cancellationToken);
        return HTTPResponse.Success(response, AuthResponseMessageDTO.LoginSuccess);
    }

    // Đổi ca nhanh bằng số điện thoại + PIN.
    public async Task<HTTPResponseData<AuthResponse?>> PinLoginAsync(PinLoginRequest request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByPhoneAsync(request.Phone.Trim(), cancellationToken);

        var valid = passwordHasher.Verify(request.Pin, user?.PinHash);
        if (user is null || !user.IsActive || !valid)
            return HTTPResponse.Fail<AuthResponse>(401, AuthResponseMessageDTO.InvalidPinCredentials);

        var response = await IssueTokensAsync(user, isLogin: true, cancellationToken);
        return HTTPResponse.Success(response, AuthResponseMessageDTO.LoginSuccess);
    }

    // Đổi refresh token lấy cặp token mới (xoay vòng). Token đã thu hồi mà bị dùng lại = nghi bị đánh cắp:
    // thu hồi toàn bộ phiên của người dùng đó.
    public async Task<HTTPResponseData<AuthResponse?>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var stored = await unitOfWork.UserRepository.GetRefreshTokenAsync(tokenService.HashRefreshToken(request.RefreshToken), cancellationToken);
        if (stored is null)
            return HTTPResponse.Fail<AuthResponse>(401, AuthResponseMessageDTO.InvalidRefreshToken);

        if (stored.RevokedAt is not null)
        {
            await unitOfWork.UserRepository.RevokeActiveRefreshTokensAsync(stored.UserId, now, cancellationToken);
            return HTTPResponse.Fail<AuthResponse>(401, AuthResponseMessageDTO.InvalidRefreshToken);
        }
        if (stored.ExpiresAt <= now || !stored.User.IsActive)
            return HTTPResponse.Fail<AuthResponse>(401, AuthResponseMessageDTO.InvalidRefreshToken);

        stored.RevokedAt = now;
        var response = await IssueTokensAsync(stored.User, isLogin: false, cancellationToken, replacing: stored);
        return HTTPResponse.Success(response, AuthResponseMessageDTO.RefreshSuccess);
    }

    // Đăng xuất: thu hồi refresh token của thiết bị này (gọi lại nhiều lần vẫn thành công).
    public async Task<HTTPResponseData<object?>> LogoutAsync(int userId, RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var stored = await unitOfWork.UserRepository.GetRefreshTokenAsync(tokenService.HashRefreshToken(request.RefreshToken), cancellationToken);
        if (stored is not null && stored.UserId == userId && stored.RevokedAt is null)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return HTTPResponse.Success<object?>(null, AuthResponseMessageDTO.LogoutSuccess);
    }

    // Lấy thông tin người dùng đang đăng nhập.
    public async Task<HTTPResponseData<CurrentUserResponse?>> GetMeAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByIdAsync(userId, cancellationToken);
        return user is null || !user.IsActive
            ? HTTPResponse.Fail<CurrentUserResponse>(401, AuthResponseMessageDTO.Unauthorized)
            : HTTPResponse.Success(Map(user), AuthResponseMessageDTO.GetMeSuccess);
    }

    // Đổi mật khẩu của chính mình; thu hồi mọi phiên để thiết bị khác phải đăng nhập lại.
    public async Task<HTTPResponseData<object?>> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.UserRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
            return HTTPResponse.Fail<object>(401, AuthResponseMessageDTO.Unauthorized);
        if (user.PasswordHash is null)
            return HTTPResponse.Fail<object>(400, AuthResponseMessageDTO.NoPasswordSet);
        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return HTTPResponse.Fail<object>(400, AuthResponseMessageDTO.WrongCurrentPassword);

        var now = DateTime.UtcNow;
        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = now;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await unitOfWork.UserRepository.RevokeActiveRefreshTokensAsync(userId, now, cancellationToken);

        return HTTPResponse.Success<object?>(null, AuthResponseMessageDTO.ChangePasswordSuccess);
    }

    // Cấp access token + refresh token mới và lưu refresh token (bản băm).
    private async Task<AuthResponse> IssueTokensAsync(User user, bool isLogin, CancellationToken cancellationToken, RefreshToken? replacing = null)
    {
        var access = tokenService.CreateAccessToken(user);
        var refresh = tokenService.CreateRefreshToken();

        var entity = new RefreshToken { TokenHash = refresh.TokenHash, ExpiresAt = refresh.ExpiresAt };
        user.RefreshTokens.Add(entity);
        if (replacing is not null) replacing.ReplacedBy = entity;
        if (isLogin) user.LastLoginAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new AuthResponse(access.Token, access.ExpiresAt, refresh.Token, refresh.ExpiresAt, Map(user));
    }

    private static CurrentUserResponse Map(User user) => new(user.Id, user.Name, user.Phone, user.Role);
}
