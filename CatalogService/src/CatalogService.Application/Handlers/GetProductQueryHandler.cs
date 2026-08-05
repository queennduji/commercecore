using CatalogService.Application.Common;
using CatalogService.Application.Dtos;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Mapping;
using CatalogService.Application.Queries;
using MediatR;

namespace CatalogService.Application.Handlers;

public class GetProductQueryHandler : IRequestHandler<GetProductQuery, ServiceResult<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductImageRepository _productImageRepository;
    private readonly ICacheService _cacheService;

    public GetProductQueryHandler(
        IProductRepository productRepository,
        IProductImageRepository productImageRepository,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
        _cacheService = cacheService;
    }

    public async Task<ServiceResult<ProductDto>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Product(request.Id);

        var cached = await _cacheService.GetAsync<ProductDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return ServiceResult<ProductDto>.Success(cached);
        }

        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Failure("Product not found.");
        }

        var images = await _productImageRepository.ListByProductIdsAsync([request.Id], cancellationToken);
        var imageDtos = images.Select(i => i.ToDto()).ToList();
        var dto = product.ToDto(imageDtos);

        await _cacheService.SetAsync(cacheKey, dto, cancellationToken: cancellationToken);

        return ServiceResult<ProductDto>.Success(dto);
    }
}
