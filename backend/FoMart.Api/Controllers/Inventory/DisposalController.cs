using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using FoMart.Application.DTO;
using FoMart.Application.DTO.Inventory;
using FoMart.Application.Services.Inventory;
using FoMart.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoMart.Api.Controllers;

// Phiếu xuất hủy (UC-08): lập phiếu, chủ tiệm duyệt / từ chối phiếu của nhân viên (BR-21).
[ApiController]
[Route("api/disposals")]
[ProducesResponseType(typeof(HTTPResponseData<object?>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(HTTPResponseData<object?>), StatusCodes.Status403Forbidden)]
public sealed class DisposalController(DisposalService disposalService) : ControllerBase
{
    // Danh sách phiếu hủy, mới nhất trước (lọc trạng thái; pending = chờ duyệt).
    [HttpGet]
    [ProducesResponseType(typeof(HTTPResponseData<PagedResult<DisposalListItemResponse>?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<PagedResult<DisposalListItemResponse>?>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HTTPResponseData<PagedResult<DisposalListItemResponse>?>>> Search(
        [FromQuery] DisposalSearchRequest request, CancellationToken cancellationToken)
    {
        var response = await disposalService.SearchAsync(request, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Chi tiết phiếu hủy kèm các lô đã trừ và tổn thất.
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(HTTPResponseData<DisposalResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<DisposalResponse?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HTTPResponseData<DisposalResponse?>>> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await disposalService.GetByIdAsync(id, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Lập phiếu hủy: chủ tiệm lập thì trừ tồn ngay (BR-15); nhân viên lập thì chờ duyệt (BR-21).
    [HttpPost]
    [ProducesResponseType(typeof(HTTPResponseData<DisposalResponse?>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HTTPResponseData<DisposalResponse?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(HTTPResponseData<DisposalResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<DisposalResponse?>>> Create(
        [FromBody] CreateDisposalRequest request, CancellationToken cancellationToken)
    {
        if (GetCurrentUserId() is not { } userId) return UnauthorizedResponse();

        var response = await disposalService.CreateAsync(request, userId, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Chủ tiệm duyệt phiếu chờ: trừ tồn theo lô, ghi tổn thất theo giá vốn.
    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = UserRole.Owner)]
    [ProducesResponseType(typeof(HTTPResponseData<DisposalResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<DisposalResponse?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HTTPResponseData<DisposalResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<DisposalResponse?>>> Approve(int id, CancellationToken cancellationToken)
    {
        if (GetCurrentUserId() is not { } userId) return UnauthorizedResponse();

        var response = await disposalService.ApproveAsync(id, userId, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Chủ tiệm từ chối phiếu chờ: tồn giữ nguyên.
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = UserRole.Owner)]
    [ProducesResponseType(typeof(HTTPResponseData<DisposalResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<DisposalResponse?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HTTPResponseData<DisposalResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<DisposalResponse?>>> Reject(
        int id, [FromBody] RejectDisposalRequest request, CancellationToken cancellationToken)
    {
        if (GetCurrentUserId() is not { } userId) return UnauthorizedResponse();

        var response = await disposalService.RejectAsync(id, request, userId, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Id người dùng hiện tại lấy từ claim "sub" của JWT; null nếu không có.
    private int? GetCurrentUserId() =>
        int.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;

    // Vai trò hiện tại (claim "role"): quyết định tự duyệt (BR-15) và có thấy giá vốn hay không (BR-08).
    private string? GetCurrentUserRole() => User.FindFirst("role")?.Value;

    private ObjectResult UnauthorizedResponse() =>
        StatusCode(StatusCodes.Status401Unauthorized,
            HTTPResponse.Fail<DisposalResponse>(StatusCodes.Status401Unauthorized, InventoryResponseMessageDTO.Unauthorized));
}
