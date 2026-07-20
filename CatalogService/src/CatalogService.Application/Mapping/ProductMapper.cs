using CatalogService.Application.Dtos;
using CatalogService.Domain.Entities;

namespace CatalogService.Application.Mapping;

public static class ProductMapper
{
    public static ProductDto ToDto(this Product product, IReadOnlyList<ProductImageDto>? images = null) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Sku,
        product.Price,
        product.Status.ToString(),
        product.CategoryId,
        product.CreatedAt,
        product.UpdatedAt,
        images ?? Array.Empty<ProductImageDto>());
}
