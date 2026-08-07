using InventoryService.Application.Commands;
using InventoryService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly ISender _mediator;

    public InventoryController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List(
        [FromQuery] Guid? productId,
        [FromQuery] Guid? locationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ListInventoryQuery(productId, locationId, page, pageSize), cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{productId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> ListByProduct(Guid productId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListInventoryByProductQuery(productId), cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{productId:guid}/{locationId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAtLocation(Guid productId, Guid locationId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetInventoryItemQuery(productId, locationId), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { errors = result.Errors });
    }

    [HttpPost("adjust")]
    [Authorize]
    public async Task<IActionResult> Adjust(AdjustStockCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    [HttpGet("reservations/{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReservation(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReservationQuery(id), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { errors = result.Errors });
    }

    [HttpPost("reservations")]
    [Authorize]
    public async Task<IActionResult> Reserve(ReserveStockCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetReservation), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("reservations/{id:guid}/release")]
    [Authorize]
    public async Task<IActionResult> Release(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ReleaseReservationCommand(id), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("reservations/{id:guid}/commit")]
    [Authorize]
    public async Task<IActionResult> Commit(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CommitReservationCommand(id), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }
}
