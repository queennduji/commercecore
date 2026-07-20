using CatalogService.Application.Common;
using MediatR;

namespace CatalogService.Application.Commands;

public record DeleteProductCommand(Guid Id) : IRequest<ServiceResult<bool>>;
