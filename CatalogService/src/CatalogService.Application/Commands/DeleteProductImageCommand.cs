using CatalogService.Application.Common;
using MediatR;

namespace CatalogService.Application.Commands;

public record DeleteProductImageCommand(Guid ProductId, Guid ImageId) : IRequest<ServiceResult<bool>>;
