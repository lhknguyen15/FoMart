using FoMart.Application.DTO;
using FoMart.Application.DTO.Catalog;
using FoMart.Application.Services.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace FoMart.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
public sealed class SupplierController(SupplierService supplierService) : ControllerBase
{
    // Tìm nhà cung cấp theo tên (không dấu) hoặc số điện thoại.
    [HttpGet]
    [ProducesResponseType(typeof(HTTPResponseData<IReadOnlyList<SupplierResponse>?>), StatusCodes.Status200OK)]
    public async Task<ActionResult<HTTPResponseData<IReadOnlyList<SupplierResponse>?>>> Search(
        [FromQuery] SupplierSearchRequest request, CancellationToken cancellationToken)
    {
        var response = await supplierService.SearchAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Lấy một nhà cung cấp.
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(HTTPResponseData<SupplierResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<SupplierResponse?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HTTPResponseData<SupplierResponse?>>> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await supplierService.GetByIdAsync(id, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Tạo nhà cung cấp.
    [HttpPost]
    [ProducesResponseType(typeof(HTTPResponseData<SupplierResponse?>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HTTPResponseData<SupplierResponse?>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HTTPResponseData<SupplierResponse?>>> Create(
        [FromBody] CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var response = await supplierService.CreateAsync(request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Sửa nhà cung cấp; IsActive = false để ẩn.
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(HTTPResponseData<SupplierResponse?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<SupplierResponse?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HTTPResponseData<SupplierResponse?>>> Update(
        int id, [FromBody] UpdateSupplierRequest request, CancellationToken cancellationToken)
    {
        var response = await supplierService.UpdateAsync(id, request, cancellationToken);
        return StatusCode(response.StatusCode, response);
    }
}
