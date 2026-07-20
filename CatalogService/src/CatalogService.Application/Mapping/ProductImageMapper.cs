using CatalogService.Application.Dtos;
using CatalogService.Domain.Entities;

namespace CatalogService.Application.Mapping;

public static class ProductImageMapper
{
    public static ProductImageDto ToDto(this ProductImage image) => new(
        image.Id,
        image.Url,
        image.SortOrder,
        image.IsPrimary);
}
