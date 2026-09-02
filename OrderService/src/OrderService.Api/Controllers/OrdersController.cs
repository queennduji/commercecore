using System.Security.Claims;
using OrderService.Application.Commands;
using OrderService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OrderService.Api.Controllers;

public record CheckoutRequest(string ShippingAddress);

public record PayRequest(string PaymentMethodId);

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ISender _mediator;

    public OrdersController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(CheckoutRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CheckoutCommand(GetUserId(), request.ShippingAddress), cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { errors = result.Errors });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrderQuery(id, GetUserId()), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { errors = result.Errors });
    }

    [HttpGet("me")]
    public async Task<IActionResult> ListMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ListMyOrdersQuery(GetUserId(), page, pageSize), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/pay")]
    public async Task<IActionResult> Pay(Guid id, PayRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new MarkOrderPaidCommand(id, GetUserId(), request.PaymentMethodId), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelOrderCommand(id, GetUserId()), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    // Ship/Deliver used to be manual ops actions here (any authenticated caller, no ownership
    // check). They're now driven by real fulfillment activity in ShippingService instead: this
    // service consumes shipment.dispatched.v1/shipment.delivered.v1 (see
    // OrderService.Infrastructure.Consumers) and dispatches ShipOrderCommand/DeliverOrderCommand
    // itself – the handlers are unchanged, only the trigger moved from an HTTP endpoint to a Kafka
    // consumer. Refund remains an ops action: real role-gating now (an interim ownership check
    // lived here briefly, before role infrastructure existed - removed now that it does, since an
    // admin refunding a customer's order was never going to *be* that order's owner).
    [HttpPost("{id:guid}/refund")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Refund(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RefundOrderCommand(id), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    private Guid GetUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.Parse(subject!);
    }
}
