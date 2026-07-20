using CatalogService.Application.Common;
using CatalogService.Application.Dtos;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Mapping;
using CatalogService.Application.Queries;
using MediatR;

namespace CatalogService.Application.Handlers;

public class ListProductsQueryHandler : IRequestHandler<ListProductsQuery, ServiceResult<PagedResult<ProductDto>>>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductImageRepository _productImageRepository;

    public ListProductsQueryHandler(IProductRepository productRepository, IProductImageRepository productImageRepository)
    {
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
    }

    public async Task<ServiceResult<PagedResult<ProductDto>>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _productRepository.ListAsync(
            request.CategoryId,
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);

        var images = await _productImageRepository.ListByProductIdsAsync(items.Select(p => p.Id), cancellationToken);
        var imagesByProductId = images.ToLookup(i => i.ProductId);

        var dtos = items.Select(p => p.ToDto(imagesByProductId[p.Id].Select(i => i.ToDto()).ToList())).ToList();
        var result = new PagedResult<ProductDto>(dtos, request.Page, request.PageSize, totalCount);

        return ServiceResult<PagedResult<ProductDto>>.Success(result);
    }
}
