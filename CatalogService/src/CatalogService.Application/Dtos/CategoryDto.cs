namespace CatalogService.Application.Dtos;

public record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
