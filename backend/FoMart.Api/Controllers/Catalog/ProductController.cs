using System.Globalization;
using System.Security.Claims;
using FoMart.Application.DTO;
using FoMart.Application.DTO.Catalog;
using FoMart.Application.Services.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace FoMart.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductController(ProductService productService) : ControllerBase
{
    // Tìm sản phẩm theo tên (không dấu) hoặc mã vạch, lọc nhóm hàng, trạng thái; có phân trang.
    [HttpGet]
    [ProducesResponseType(typeof(HTTPResponseData<PagedResult<ProductListItemResponse>?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<PagedResult<ProductListItemResponse>?>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HTTPResponseData<PagedResult<ProductListItemResponse>?>>> Search(
        [FromQuery] ProductSearchRequest request, CancellationToken cancellationToken)
    {
        var response = await productService.SearchAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Lấy chi tiết sản phẩm: đơn vị, tồn theo vị trí.
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(HTTPResponseData<ProductResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<ProductResponse?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HTTPResponseData<ProductResponse?>>> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await productService.GetByIdAsync(id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Tra mã vạch (quét camera hoặc máy quét): trả sản phẩm và đơn vị mang mã đó.
    [HttpGet("barcode/{barcode}")]
    [ProducesResponseType(typeof(HTTPResponseData<ProductBarcodeResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<ProductBarcodeResponse?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HTTPResponseData<ProductBarcodeResponse?>>> GetByBarcode(string barcode, CancellationToken cancellationToken)
    {
        var response = await productService.GetByBarcodeAsync(barcode, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Tạo sản phẩm kèm các đơn vị (đúng một đơn vị cơ sở).
    [HttpPost]
    [ProducesResponseType(typeof(HTTPResponseData<ProductResponse?>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HTTPResponseData<ProductResponse?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(HTTPResponseData<ProductResponse?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(HTTPResponseData<ProductResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<ProductResponse?>>> Create(
        [FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (GetCurrentUserId() is not { } userId) return UnauthorizedResponse();

        var response = await productService.CreateAsync(request, userId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Sửa thông tin chung; Status = inactive để ngừng bán.
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(HTTPResponseData<ProductResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<ProductResponse?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(HTTPResponseData<ProductResponse?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HTTPResponseData<ProductResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<ProductResponse?>>> Update(
        int id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var response = await productService.UpdateAsync(id, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Thêm một đơn vị (lốc, thùng...) cho sản phẩm.
    [HttpPost("{id:int}/units")]
    [ProducesResponseType(typeof(HTTPResponseData<ProductResponse?>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HTTPResponseData<ProductResponse?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(HTTPResponseData<ProductResponse?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HTTPResponseData<ProductResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<ProductResponse?>>> AddUnit(
        int id, [FromBody] AddProductUnitRequest request, CancellationToken cancellationToken)
    {
        if (GetCurrentUserId() is not { } userId) return UnauthorizedResponse();

        var response = await productService.AddUnitAsync(id, request, userId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Sửa một đơn vị: tên, mã vạch, giá (ghi lịch sử giá), hệ số (khóa khi đã có giao dịch).
    [HttpPut("{id:int}/units/{unitId:int}")]
    [ProducesResponseType(typeof(HTTPResponseData<ProductResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<ProductResponse?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(HTTPResponseData<ProductResponse?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(HTTPResponseData<ProductResponse?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HTTPResponseData<ProductResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<ProductResponse?>>> UpdateUnit(
        int id, int unitId, [FromBody] UpdateProductUnitRequest request, CancellationToken cancellationToken)
    {
        if (GetCurrentUserId() is not { } userId) return UnauthorizedResponse();

        var response = await productService.UpdateUnitAsync(id, unitId, request, userId, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Id người dùng hiện tại lấy từ claim; null nếu chưa đăng nhập.
    private int? GetCurrentUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;

    private ObjectResult UnauthorizedResponse() =>
        StatusCode(StatusCodes.Status401Unauthorized,
            HTTPResponse.Fail<ProductResponse>(StatusCodes.Status401Unauthorized, CatalogResponseMessageDTO.Unauthorized));
}
