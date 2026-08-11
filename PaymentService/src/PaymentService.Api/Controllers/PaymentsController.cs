using System.Security.Claims;
using PaymentService.Application.Commands;
using PaymentService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PaymentService.Api.Controllers;

public record ChargeRequest(Guid OrderId, decimal Amount, string Currency, string PaymentMethodId);

public record RefundRequest(Guid OrderId);

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly ISender _mediator;

    public PaymentsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("charge")]
    public async Task<IActionResult> Charge(ChargeRequest request, CancellationToken cancellationToken)
    {
        var command = new ChargeCommand(request.OrderId, GetUserId(), request.Amount, request.Currency, request.PaymentMethodId);
        var result = await _mediator.Send(command, cancellationToken);
        return result.Succeeded
            ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value)
            : BadRequest(new { errors = result.Errors });
    }

    // Ops action — called by OrderService's own ops-only Refund transition, no ownership check
    // here either (mirrors Order's Ship/Deliver/Refund pattern).
    [HttpPost("refund")]
    public async Task<IActionResult> Refund(RefundRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RefundCommand(request.OrderId), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPaymentQuery(id, GetUserId()), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { errors = result.Errors });
    }

    [HttpGet("order/{orderId:guid}")]
    public async Task<IActionResult> ListByOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListPaymentsByOrderQuery(orderId, GetUserId()), cancellationToken);
        return Ok(result.Value);
    }

    private Guid GetUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.Parse(subject!);
    }
}
