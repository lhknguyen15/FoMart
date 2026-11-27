using FoMart.Application.DTO;
using FoMart.Application.DTO.Catalog;
using FoMart.Application.Services.Catalog;
using FoMart.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoMart.Api.Controllers;

[ApiController]
[Route("api/categories")]
[ProducesResponseType(typeof(HTTPResponseData<object?>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(HTTPResponseData<object?>), StatusCodes.Status403Forbidden)]
public sealed class CategoryController(CategoryService categoryService) : ControllerBase
{
    // Lấy danh sách nhóm hàng; includeInactive = true để thấy cả nhóm đã ẩn.
    [HttpGet]
    [ProducesResponseType(typeof(HTTPResponseData<IReadOnlyList<CategoryResponse>?>), StatusCodes.Status200OK)]
    public async Task<ActionResult<HTTPResponseData<IReadOnlyList<CategoryResponse>?>>> GetAll(
        [FromQuery] bool includeInactive, CancellationToken cancellationToken)
    {
        var response = await categoryService.GetAllAsync(includeInactive, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Tạo nhóm hàng.
    [HttpPost]
    [Authorize(Roles = UserRole.Owner)]
    [ProducesResponseType(typeof(HTTPResponseData<CategoryResponse?>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HTTPResponseData<CategoryResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<CategoryResponse?>>> Create(
        [FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var response = await categoryService.CreateAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Sửa nhóm hàng; IsActive = false để ẩn.
    [HttpPut("{id:int}")]
    [Authorize(Roles = UserRole.Owner)]
    [ProducesResponseType(typeof(HTTPResponseData<CategoryResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<CategoryResponse?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(HTTPResponseData<CategoryResponse?>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HTTPResponseData<CategoryResponse?>>> Update(
        int id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var response = await categoryService.UpdateAsync(id, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
