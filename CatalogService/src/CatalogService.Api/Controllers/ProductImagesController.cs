using CatalogService.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Api.Controllers;

[ApiController]
[Route("api/products/{productId:guid}/images")]
public class ProductImagesController : ControllerBase
{
    private readonly ISender _mediator;

    public ProductImagesController(ISender mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("upload-url")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RequestUploadUrl(Guid productId, [FromBody] RequestProductImageUploadCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { ProductId = productId }, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Attach(Guid productId, [FromBody] AttachProductImageCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { ProductId = productId }, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{imageId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid productId, Guid imageId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteProductImageCommand(productId, imageId), cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }
}
