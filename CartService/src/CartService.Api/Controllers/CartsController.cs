using System.Security.Claims;
using CartService.Application.Commands;
using CartService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CartService.Api.Controllers;

public record MergeCartRequest(Guid SourceCartId);

[ApiController]
[Route("api/carts")]
public class CartsController : ControllerBase
{
    private readonly ISender _mediator;

    public CartsController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateCartCommand(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCartQuery(id), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : NotFound(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Clear(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ClearCartCommand(id), cancellationToken);
        return result.Succeeded ? NoContent() : NotFound(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/items")]
    [AllowAnonymous]
    public async Task<IActionResult> AddItem(Guid id, AddCartItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AddCartItemCommand(id, request.ProductId, request.Quantity), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    [HttpPut("{id:guid}/items/{productId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateItemQuantity(Guid id, Guid productId, UpdateCartItemQuantityRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateCartItemQuantityCommand(id, productId, request.Quantity), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}/items/{productId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> RemoveItem(Guid id, Guid productId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RemoveCartItemCommand(id, productId), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyCart(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrCreateUserCartCommand(GetUserId()), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost("me/merge")]
    [Authorize]
    public async Task<IActionResult> MergeIntoMyCart(MergeCartRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new MergeCartCommand(GetUserId(), request.SourceCartId), cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    private Guid GetUserId()
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.Parse(subject!);
    }
}

public record AddCartItemRequest(Guid ProductId, int Quantity);

public record UpdateCartItemQuantityRequest(int Quantity);
