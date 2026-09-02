using System.Security.Claims;
using ShippingService.Application.Commands;
using ShippingService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ShippingService.Api.Controllers;

public record DispatchRequest(string Carrier, string TrackingCode);

[ApiController]
[Route("api/shipments")]
[Authorize]
public class ShipmentsController : ControllerBase
{
    private readonly ISender _mediator;

    public ShipmentsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetShipmentQuery(id, GetUserId()), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { errors = result.Errors });
    }

    [HttpGet("order/{orderId:guid}")]
    public async Task<IActionResult> GetByOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetShipmentByOrderQuery(orderId, GetUserId()), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { errors = result.Errors });
    }

    // Ops actions – gated to fulfillment/admin staff via role, not an ownership check (these
    // aren't a customer acting on their own shipment). Previously any authenticated caller could
    // reach these, back when no role system existed yet.
    [HttpPost("{id:guid}/dispatch")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Dispatch(Guid id, DispatchRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DispatchShipmentCommand(id, request.Carrier, request.TrackingCode), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/refresh-tracking")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RefreshTracking(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RefreshShipmentTrackingCommand(id), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    private Guid GetUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.Parse(subject!);
    }
}
