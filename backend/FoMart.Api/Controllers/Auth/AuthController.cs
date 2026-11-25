using System.Globalization;
using FoMart.Application.DTO;
using FoMart.Application.DTO.Auth;
using FoMart.Application.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FoMart.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    // Tạo chủ tiệm đầu tiên khi cửa hàng chưa có chủ tiệm nào; trả luôn token đăng nhập.
    [HttpPost("setup")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(HTTPResponseData<AuthResponse?>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HTTPResponseData<AuthResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<AuthResponse?>>> Setup(
        [FromBody] SetupOwnerRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.SetupOwnerAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Đăng nhập bằng số điện thoại + mật khẩu.
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(HTTPResponseData<AuthResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<AuthResponse?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(HTTPResponseData<object?>), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<HTTPResponseData<AuthResponse?>>> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Đổi ca nhanh bằng số điện thoại + PIN.
    [HttpPost("login-pin")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(HTTPResponseData<AuthResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<AuthResponse?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(HTTPResponseData<object?>), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<HTTPResponseData<AuthResponse?>>> PinLogin(
        [FromBody] PinLoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.PinLoginAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Đổi refresh token lấy cặp token mới. Không giới hạn tần suất: refresh token 64 byte ngẫu nhiên, không dò được.
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HTTPResponseData<AuthResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<AuthResponse?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<HTTPResponseData<AuthResponse?>>> Refresh(
        [FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.RefreshAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Đăng xuất thiết bị hiện tại (thu hồi refresh token).
    [HttpPost("logout")]
    [ProducesResponseType(typeof(HTTPResponseData<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<HTTPResponseData<object?>>> Logout(
        [FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (GetCurrentUserId() is not { } userId) return UnauthorizedResponse();

        var response = await authService.LogoutAsync(userId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Lấy thông tin người dùng đang đăng nhập.
    [HttpGet("me")]
    [ProducesResponseType(typeof(HTTPResponseData<CurrentUserResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<CurrentUserResponse?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<HTTPResponseData<CurrentUserResponse?>>> GetMe(CancellationToken cancellationToken)
    {
        if (GetCurrentUserId() is not { } userId) return UnauthorizedResponse();

        var response = await authService.GetMeAsync(userId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Đổi mật khẩu của chính mình.
    [HttpPut("password")]
    [ProducesResponseType(typeof(HTTPResponseData<object?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(HTTPResponseData<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<HTTPResponseData<object?>>> ChangePassword(
        [FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (GetCurrentUserId() is not { } userId) return UnauthorizedResponse();

        var response = await authService.ChangePasswordAsync(userId, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Id người dùng hiện tại lấy từ claim "sub" của JWT; null nếu không có.
    private int? GetCurrentUserId() =>
        int.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;

    private ObjectResult UnauthorizedResponse() =>
        StatusCode(StatusCodes.Status401Unauthorized,
            HTTPResponse.Fail<object>(StatusCodes.Status401Unauthorized, AuthResponseMessageDTO.Unauthorized));
}
