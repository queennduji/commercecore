using CatalogService.Application.Common;
using CatalogService.Application.Dtos;
using MediatR;

namespace CatalogService.Application.Queries;

public record ListCategoriesQuery : IRequest<ServiceResult<IReadOnlyList<CategoryDto>>>;
