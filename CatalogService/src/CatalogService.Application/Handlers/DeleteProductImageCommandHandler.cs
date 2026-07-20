using CatalogService.Application.Commands;
using CatalogService.Application.Common;
using CatalogService.Application.Interfaces;
using MediatR;

namespace CatalogService.Application.Handlers;

public class DeleteProductImageCommandHandler : IRequestHandler<DeleteProductImageCommand, ServiceResult<bool>>
{
    private readonly IProductImageRepository _productImageRepository;
    private readonly IBlobStorageService _blobStorageService;

    public DeleteProductImageCommandHandler(IProductImageRepository productImageRepository, IBlobStorageService blobStorageService)
    {
        _productImageRepository = productImageRepository;
        _blobStorageService = blobStorageService;
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

        return ServiceResult<bool>.Success(true);
    }
}
