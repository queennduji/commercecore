using CatalogService.Application.Common;
using CatalogService.Application.Dtos;
using MediatR;

namespace CatalogService.Application.Queries;

public record GetCategoryQuery(Guid Id) : IRequest<ServiceResult<CategoryDto>>;
