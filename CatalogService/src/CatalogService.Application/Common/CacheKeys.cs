using CatalogService.Domain.Entities;

namespace CatalogService.Application.Common;

public static class CacheKeys
{
    public const string ProductListPrefix = "catalog:products:list:";

    public static string Product(Guid productId) => $"catalog:product:{productId}";

    public static string ProductList(Guid? categoryId, ProductStatus? status, int page, int pageSize) =>
        $"{ProductListPrefix}{categoryId}:{status}:{page}:{pageSize}";
}
