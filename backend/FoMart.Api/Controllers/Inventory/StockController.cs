using FoMart.Application.DTO;
using FoMart.Application.DTO.Inventory;
using FoMart.Application.Services.Inventory;
using FoMart.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoMart.Api.Controllers;

// Xem tồn kho: tồn theo lô, thẻ kho, đối soát sổ kho. Chỉ đọc; tồn chỉ đổi qua chứng từ (BR-02).
[ApiController]
[Route("api/stock")]
[ProducesResponseType(typeof(HTTPResponseData<object?>), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(HTTPResponseData<object?>), StatusCodes.Status403Forbidden)]
public sealed class StockController(StockService stockService) : ControllerBase
{
    // Tồn theo lô và vị trí (lọc sản phẩm, vị trí); lô hết hạn sớm đứng trước.
    [HttpGet]
    [ProducesResponseType(typeof(HTTPResponseData<PagedResult<StockLotResponse>?>), StatusCodes.Status200OK)]
    public async Task<ActionResult<HTTPResponseData<PagedResult<StockLotResponse>?>>> Search(
        [FromQuery] StockSearchRequest request, CancellationToken cancellationToken)
    {
        var response = await stockService.SearchAsync(request, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Thẻ kho của một sản phẩm: từng biến động, chứng từ gốc, tồn sau biến động.
    [HttpGet("moves")]
    [ProducesResponseType(typeof(HTTPResponseData<PagedResult<StockMoveResponse>?>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HTTPResponseData<PagedResult<StockMoveResponse>?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(HTTPResponseData<PagedResult<StockMoveResponse>?>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HTTPResponseData<PagedResult<StockMoveResponse>?>>> GetMoves(
        [FromQuery] StockMoveSearchRequest request, CancellationToken cancellationToken)
    {
        var response = await stockService.GetMovesAsync(request, GetCurrentUserRole(), cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Đối soát tồn với sổ kho; danh sách rỗng là khớp.
    [HttpGet("ledger-check")]
    [Authorize(Roles = UserRole.Owner)]
    [ProducesResponseType(typeof(HTTPResponseData<IReadOnlyList<StockLedgerMismatchResponse>?>), StatusCodes.Status200OK)]
    public async Task<ActionResult<HTTPResponseData<IReadOnlyList<StockLedgerMismatchResponse>?>>> CheckLedger(CancellationToken cancellationToken)
    {
        var response = await stockService.CheckLedgerAsync(cancellationToken);
        return StatusCode(response.StatusCode, response);
    }

    // Vai trò hiện tại (claim "role"): quyết định có thấy giá vốn hay không (BR-08).
    private string? GetCurrentUserRole() => User.FindFirst("role")?.Value;
}
