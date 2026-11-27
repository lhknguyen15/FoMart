using FoMart.Application.DTO;
using FoMart.Application.DTO.Auth;
using FoMart.Application.Services.Auth;
using FoMart.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoMart.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = UserRole.Owner)]
[ProducesResponseType(typeof(HTTPResponseData<object?>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(HTTPResponseData<object?>), StatusCodes.Status403Forbidden)]
public sealed class UserController(UserService userService) : ControllerBase
{
    // Lấy danh sách tài khoản; includeInactive = true để thấy cả tài khoản đã khóa.
    [HttpGet]
    [ProducesResponseType(typeof(HTTPResponseData<IReadOnlyList<UserResponse>?>), StatusCodes.Status200OK)]
    public async Task<ActionResult<HTTPResponseData<IReadOnlyList<UserResponse>?>>> GetAll(
        [FromQuery] bool includeInactive, CancellationToken cancellationToken)
    {
        var response = await userService.GetAllAsync(includeInactive, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Tạo tài khoản chủ tiệm hoặc nhân viên.
    [HttpPost]
    [ProducesResponseType(typeof(HTTPResponseData<UserResponse?>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HTTPResponseData<UserResponse?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(HTTPResponseData<UserResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<UserResponse?>>> Create(
        [FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var response = await userService.CreateAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Đổi tên, khóa/mở khóa tài khoản.
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(HTTPResponseData<UserResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<UserResponse?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HTTPResponseData<UserResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<UserResponse?>>> Update(
        int id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var response = await userService.UpdateAsync(id, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Đặt lại PIN cho một tài khoản.
    [HttpPut("{id:int}/pin")]
    [ProducesResponseType(typeof(HTTPResponseData<UserResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<UserResponse?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HTTPResponseData<UserResponse?>>> ResetPin(
        int id, [FromBody] ResetPinRequest request, CancellationToken cancellationToken)
    {
        var response = await userService.ResetPinAsync(id, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
