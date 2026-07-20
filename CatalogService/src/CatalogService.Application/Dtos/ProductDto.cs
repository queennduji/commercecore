namespace CatalogService.Application.Dtos;

public record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    string Sku,
    decimal Price,
    string Status,
    Guid CategoryId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ProductImageDto> Images);
