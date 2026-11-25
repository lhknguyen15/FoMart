using FoMart.Application.DTO;
using FoMart.Application.DTO.Catalog;
using FoMart.Application.Services.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace FoMart.Api.Controllers;

[ApiController]
[Route("api/categories")]
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
