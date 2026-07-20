using CatalogService.Application.Common;
using CatalogService.Application.Dtos;
using CatalogService.Domain.Entities;
using MediatR;

namespace CatalogService.Application.Queries;

public record ListProductsQuery(
    Guid? CategoryId,
    ProductStatus? Status,
    int Page = 1,
    int PageSize = 20) : IRequest<ServiceResult<PagedResult<ProductDto>>>;
