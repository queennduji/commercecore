using CatalogService.Application.Commands;
using CatalogService.Application.Common;
using CatalogService.Application.Dtos;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Mapping;
using CatalogService.Domain.Entities;
using MediatR;

namespace CatalogService.Application.Handlers;

public class AttachProductImageCommandHandler : IRequestHandler<AttachProductImageCommand, ServiceResult<ProductImageDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductImageRepository _productImageRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ICacheService _cacheService;

    public AttachProductImageCommandHandler(
        IProductRepository productRepository,
        IProductImageRepository productImageRepository,
        IBlobStorageService blobStorageService,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
        _blobStorageService = blobStorageService;
        _cacheService = cacheService;
    }

    public async Task<ServiceResult<ProductImageDto>> Handle(AttachProductImageCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return ServiceResult<ProductImageDto>.Failure("Product not found.");
        }

        var publicUrl = _blobStorageService.GetPublicUrl(request.ObjectKey);

        var image = new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            ObjectKey = request.ObjectKey,
            Url = publicUrl,
            SortOrder = request.SortOrder,
            IsPrimary = request.IsPrimary,
            CreatedAt = DateTime.UtcNow
        };

        await _productImageRepository.AddAsync(image, cancellationToken);
        await _productImageRepository.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync(CacheKeys.Product(request.ProductId), cancellationToken);
        await _cacheService.RemoveByPrefixAsync(CacheKeys.ProductListPrefix, cancellationToken);

        return ServiceResult<ProductImageDto>.Success(image.ToDto());
    }
}
