using System.ComponentModel.DataAnnotations;
using System.Globalization;
using FoMart.Application.DTO;
using FoMart.Application.DTO.Purchasing;
using FoMart.Application.Services.Purchasing;
using FoMart.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FoMart.Api.Controllers;

// Phiếu nhập (UC-04). Chủ tiệm và nhân viên đều nhập hàng được; chỉ chủ tiệm hủy phiếu đã hoàn tất.
[ApiController]
[Route("api/goods-receipts")]
[ProducesResponseType(typeof(HTTPResponseData<object?>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(HTTPResponseData<object?>), StatusCodes.Status403Forbidden)]
public sealed class GoodsReceiptController(GoodsReceiptService goodsReceiptService) : ControllerBase
{
    // Tìm phiếu nhập theo mã, trạng thái, nhà cung cấp, khoảng ngày tạo; có phân trang.
    [HttpGet]
    [ProducesResponseType(typeof(HTTPResponseData<PagedResult<GoodsReceiptListItemResponse>?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<PagedResult<GoodsReceiptListItemResponse>?>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HTTPResponseData<PagedResult<GoodsReceiptListItemResponse>?>>> Search(
        [FromQuery] GoodsReceiptSearchRequest request, CancellationToken cancellationToken)
    {
        var response = await goodsReceiptService.SearchAsync(request, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Lấy chi tiết phiếu nhập kèm các dòng.
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HTTPResponseData<GoodsReceiptResponse?>>> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await goodsReceiptService.GetByIdAsync(id, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Gợi ý giá nhập lần trước của một đơn vị.
    [HttpGet("last-price")]
    [ProducesResponseType(typeof(HTTPResponseData<LastPurchasePriceResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<LastPurchasePriceResponse?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HTTPResponseData<LastPurchasePriceResponse?>>> GetLastPrice(
        [FromQuery, Range(1, int.MaxValue)] int unitId, CancellationToken cancellationToken)
    {
        var response = await goodsReceiptService.GetLastPriceAsync(unitId, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Tạo phiếu nhập nháp, có thể kèm các dòng.
    [HttpPost]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<GoodsReceiptResponse?>>> Create(
        [FromBody] CreateGoodsReceiptRequest request, CancellationToken cancellationToken)
    {
        if (GetCurrentUserId() is not { } userId) return UnauthorizedResponse();

        var response = await goodsReceiptService.CreateAsync(request, userId, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Sửa thông tin chung của phiếu nháp: nhà cung cấp, vị trí nhận, ghi chú.
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<GoodsReceiptResponse?>>> Update(
        int id, [FromBody] UpdateGoodsReceiptRequest request, CancellationToken cancellationToken)
    {
        var response = await goodsReceiptService.UpdateAsync(id, request, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Xóa phiếu nháp (chưa đổi tồn). Phiếu đã hoàn tất thì dùng hủy.
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<GoodsReceiptResponse?>>> Delete(int id, CancellationToken cancellationToken)
    {
        var response = await goodsReceiptService.DeleteAsync(id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Thêm một dòng nhập (đơn vị, số lượng, giá, hạn dùng, số lô).
    [HttpPost("{id:int}/lines")]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<GoodsReceiptResponse?>>> AddLine(
        int id, [FromBody] ReceiptLineRequest request, CancellationToken cancellationToken)
    {
        var response = await goodsReceiptService.AddLineAsync(id, request, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Sửa một dòng nhập của phiếu nháp.
    [HttpPut("{id:int}/lines/{lineId:int}")]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<GoodsReceiptResponse?>>> UpdateLine(
        int id, int lineId, [FromBody] ReceiptLineRequest request, CancellationToken cancellationToken)
    {
        var response = await goodsReceiptService.UpdateLineAsync(id, lineId, request, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Xóa một dòng nhập của phiếu nháp.
    [HttpDelete("{id:int}/lines/{lineId:int}")]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<GoodsReceiptResponse?>>> DeleteLine(
        int id, int lineId, CancellationToken cancellationToken)
    {
        var response = await goodsReceiptService.DeleteLineAsync(id, lineId, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Hoàn tất phiếu: tạo lô, ghi sổ kho, cộng tồn tại vị trí nhận.
    [HttpPost("{id:int}/complete")]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<GoodsReceiptResponse?>>> Complete(
        int id, [FromBody] CompleteGoodsReceiptRequest request, CancellationToken cancellationToken)
    {
        if (GetCurrentUserId() is not { } userId) return UnauthorizedResponse();

        var response = await goodsReceiptService.CompleteAsync(id, request, userId, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Hủy phiếu đã hoàn tất bằng chứng từ đảo ngược; chỉ khi hàng của phiếu chưa bị trừ.
    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = UserRole.Owner)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HTTPResponseData<GoodsReceiptResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<GoodsReceiptResponse?>>> Cancel(
        int id, [FromBody] CancelGoodsReceiptRequest request, CancellationToken cancellationToken)
    {
        if (GetCurrentUserId() is not { } userId) return UnauthorizedResponse();

        var response = await goodsReceiptService.CancelAsync(id, request, userId, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Id người dùng hiện tại lấy từ claim "sub" của JWT; null nếu không có.
    private int? GetCurrentUserId() =>
        int.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;

    // Vai trò hiện tại (claim "role"): quyết định có thấy giá nhập hay không (BR-08).
    private string? GetCurrentUserRole() => User.FindFirst("role")?.Value;

    private ObjectResult UnauthorizedResponse() =>
        StatusCode(StatusCodes.Status401Unauthorized,
            HTTPResponse.Fail<GoodsReceiptResponse>(StatusCodes.Status401Unauthorized, PurchasingResponseMessageDTO.Unauthorized));
}
