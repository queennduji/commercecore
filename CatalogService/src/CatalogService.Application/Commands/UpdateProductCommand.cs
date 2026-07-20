using CatalogService.Application.Common;
using CatalogService.Application.Dtos;
using CatalogService.Domain.Entities;
using MediatR;

namespace CatalogService.Application.Commands;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    ProductStatus Status,
    Guid CategoryId) : IRequest<ServiceResult<ProductDto>>;
