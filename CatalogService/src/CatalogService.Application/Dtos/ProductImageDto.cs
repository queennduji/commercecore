namespace CatalogService.Application.Dtos;

public record ProductImageDto(Guid Id, string Url, int SortOrder, bool IsPrimary);
