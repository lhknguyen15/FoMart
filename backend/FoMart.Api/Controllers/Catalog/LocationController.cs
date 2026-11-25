using FoMart.Application.DTO;
using FoMart.Application.DTO.Catalog;
using FoMart.Application.Services.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace FoMart.Api.Controllers;

[ApiController]
[Route("api/locations")]
public sealed class LocationController(LocationService locationService) : ControllerBase
{
    // Lấy danh sách vị trí kèm tổng tồn; vị trí bán mặc định đứng đầu.
    [HttpGet]
    [ProducesResponseType(typeof(HTTPResponseData<IReadOnlyList<LocationResponse>?>), StatusCodes.Status200OK)]
    public async Task<ActionResult<HTTPResponseData<IReadOnlyList<LocationResponse>?>>> GetAll(
        [FromQuery] bool includeInactive, CancellationToken cancellationToken)
    {
        var response = await locationService.GetAllAsync(includeInactive, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Tạo vị trí (storage = kho, shelf = kệ bán).
    [HttpPost]
    [ProducesResponseType(typeof(HTTPResponseData<LocationResponse?>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HTTPResponseData<LocationResponse?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(HTTPResponseData<LocationResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<LocationResponse?>>> Create(
        [FromBody] CreateLocationRequest request, CancellationToken cancellationToken)
    {
        var response = await locationService.CreateAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Sửa vị trí, chọn vị trí bán mặc định, ngừng dùng khi tồn bằng 0.
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(HTTPResponseData<LocationResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<LocationResponse?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(HTTPResponseData<LocationResponse?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HTTPResponseData<LocationResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<LocationResponse?>>> Update(
        int id, [FromBody] UpdateLocationRequest request, CancellationToken cancellationToken)
    {
        var response = await locationService.UpdateAsync(id, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
