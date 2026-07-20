using CatalogService.Application.Common;
using MediatR;

namespace CatalogService.Application.Commands;

public record DeleteCategoryCommand(Guid Id) : IRequest<ServiceResult<bool>>;
