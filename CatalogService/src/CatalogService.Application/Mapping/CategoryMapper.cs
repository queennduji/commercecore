using CatalogService.Application.Dtos;
using CatalogService.Domain.Entities;

namespace CatalogService.Application.Mapping;

public static class CategoryMapper
{
    public static CategoryDto ToDto(this Category category) => new(
        category.Id,
        category.Name,
        category.Description,
        category.ParentCategoryId,
        category.CreatedAt,
        category.UpdatedAt);
}
