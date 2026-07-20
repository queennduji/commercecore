using CatalogService.Application.Common;
using CatalogService.Application.Dtos;
using MediatR;

namespace CatalogService.Application.Commands;

public record CreateProductCommand(
    string Name,
    string? Description,
    string Sku,
    decimal Price,
    Guid CategoryId) : IRequest<ServiceResult<ProductDto>>;
