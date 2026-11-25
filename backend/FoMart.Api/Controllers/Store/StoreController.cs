using FoMart.Application.DTO;
using FoMart.Application.DTO.Store;
using FoMart.Application.Services.Store;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoMart.Api.Controllers;

[ApiController]
[Route("api/stores")]
public sealed class StoreController(StoreService storeService) : ControllerBase
{
    // Lấy thông tin cửa hàng (tên, địa chỉ, đã có tài khoản VietQR chưa).
    [HttpGet]
    [AllowAnonymous] // màn hình đăng nhập cần hiện tên tiệm
    [ProducesResponseType(typeof(HTTPResponseData<StoreResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<StoreResponse?>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<HTTPResponseData<StoreResponse?>>> GetStore(CancellationToken cancellationToken)
    {
        var response = await storeService.GetStoreAsync(cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
