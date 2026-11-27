using System.Globalization;
using FoMart.Application.DTO;
using FoMart.Application.DTO.Sales;
using FoMart.Application.Services.Sales;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FoMart.Api.Controllers;

// Hóa đơn bán hàng (UC-05) và hủy hóa đơn (UC-06). Chủ tiệm và nhân viên đều bán được;
// quyền hủy hóa đơn đã thanh toán (chỉ chủ tiệm) kiểm trong service vì còn tùy trạng thái hóa đơn.
[ApiController]
[Route("api/sales")]
[ProducesResponseType(typeof(HTTPResponseData<object?>), StatusCodes.Status401Unauthorized)]
public sealed class SaleController(SaleService saleService) : ControllerBase
{
    // Tìm hóa đơn theo mã, trạng thái, hình thức thanh toán, người bán, khoảng ngày; có phân trang.
    [HttpGet]
    [ProducesResponseType(typeof(HTTPResponseData<PagedResult<SaleListItemResponse>?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<PagedResult<SaleListItemResponse>?>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HTTPResponseData<PagedResult<SaleListItemResponse>?>>> Search(
        [FromQuery] SaleSearchRequest request, CancellationToken cancellationToken)
    {
        var response = await saleService.SearchAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Lấy chi tiết hóa đơn: các dòng, lô đã trừ; giá vốn và lãi chỉ chủ tiệm thấy.
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(HTTPResponseData<SaleResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<SaleResponse?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HTTPResponseData<SaleResponse?>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await saleService.GetByIdAsync(id, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Lưu hóa đơn với Id UUID v7 do máy bán sinh. Tiền mặt: trừ kho ngay. Chuyển khoản: chờ "Đã nhận".
    [HttpPost]
    [ProducesResponseType(typeof(HTTPResponseData<SaleResponse?>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HTTPResponseData<SaleResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<SaleResponse?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(HTTPResponseData<SaleResponse?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HTTPResponseData<SaleResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<SaleResponse?>>> Create(
        [FromBody] CreateSaleRequest request, CancellationToken cancellationToken)
    {
        if (GetCurrentUserId() is not { } userId) return UnauthorizedResponse();

        var response = await saleService.CreateAsync(request, userId, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Người bán bấm "Đã nhận" tiền chuyển khoản: thanh toán và trừ kho.
    [HttpPost("{id:guid}/confirm-payment")]
    [ProducesResponseType(typeof(HTTPResponseData<SaleResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<SaleResponse?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HTTPResponseData<SaleResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<SaleResponse?>>> ConfirmPayment(
        Guid id, [FromBody] ConfirmSalePaymentRequest request, CancellationToken cancellationToken)
    {
        if (GetCurrentUserId() is not { } userId) return UnauthorizedResponse();

        var response = await saleService.ConfirmPaymentAsync(id, request, userId, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Hủy hóa đơn: bỏ hóa đơn chờ thanh toán, hoặc chủ tiệm hủy hóa đơn đã thanh toán (cộng tồn lại đúng lô).
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(HTTPResponseData<SaleResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<SaleResponse?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(HTTPResponseData<SaleResponse?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(HTTPResponseData<SaleResponse?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HTTPResponseData<SaleResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<SaleResponse?>>> Cancel(
        Guid id, [FromBody] CancelSaleRequest request, CancellationToken cancellationToken)
    {
        if (GetCurrentUserId() is not { } userId) return UnauthorizedResponse();

        var response = await saleService.CancelAsync(id, request, userId, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Id người dùng hiện tại lấy từ claim "sub" của JWT; null nếu không có.
    private int? GetCurrentUserId() =>
        int.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;

    // Vai trò hiện tại (claim "role"): quyền hủy hóa đơn đã thanh toán, xem giá vốn (BR-08).
    private string? GetCurrentUserRole() => User.FindFirst("role")?.Value;

    private ObjectResult UnauthorizedResponse() =>
        StatusCode(StatusCodes.Status401Unauthorized,
            HTTPResponse.Fail<SaleResponse>(StatusCodes.Status401Unauthorized, SalesResponseMessageDTO.Unauthorized));
}
