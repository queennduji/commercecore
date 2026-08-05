using CatalogService.Application.Commands;
using CatalogService.Application.Common;
using CatalogService.Application.Interfaces;
using MediatR;

namespace CatalogService.Application.Handlers;

public class DeleteProductImageCommandHandler : IRequestHandler<DeleteProductImageCommand, ServiceResult<bool>>
{
    private readonly IProductImageRepository _productImageRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ICacheService _cacheService;

    public DeleteProductImageCommandHandler(
        IProductImageRepository productImageRepository,
        IBlobStorageService blobStorageService,
        ICacheService cacheService)
    {
        _productImageRepository = productImageRepository;
        _blobStorageService = blobStorageService;
        _cacheService = cacheService;
    }

    public async Task<ServiceResult<bool>> Handle(DeleteProductImageCommand request, CancellationToken cancellationToken)
    {
        var image = await _productImageRepository.GetByIdAsync(request.ImageId, cancellationToken);
        if (image is null || image.ProductId != request.ProductId)
        {
            return ServiceResult<bool>.Failure("Product image not found.");
        }

        _productImageRepository.Remove(image);
        await _productImageRepository.SaveChangesAsync(cancellationToken);

        await _blobStorageService.DeleteAsync(image.ObjectKey, cancellationToken);

        await _cacheService.RemoveAsync(CacheKeys.Product(image.ProductId), cancellationToken);
        await _cacheService.RemoveByPrefixAsync(CacheKeys.ProductListPrefix, cancellationToken);

        return ServiceResult<bool>.Success(true);
    }
}
