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

    public GetProductQueryHandler(IProductRepository productRepository, IProductImageRepository productImageRepository)
    {
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
    }

    public async Task<ServiceResult<ProductDto>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Failure("Product not found.");
        }

        var images = await _productImageRepository.ListByProductIdsAsync([request.Id], cancellationToken);
        var imageDtos = images.Select(i => i.ToDto()).ToList();

        return ServiceResult<ProductDto>.Success(product.ToDto(imageDtos));
    }
}
