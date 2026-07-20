using CatalogService.Application.Commands;
using CatalogService.Application.Common;
using CatalogService.Application.Interfaces;
using MediatR;

namespace CatalogService.Application.Handlers;

public class RequestProductImageUploadCommandHandler : IRequestHandler<RequestProductImageUploadCommand, ServiceResult<PresignedUploadUrl>>
{
    private readonly IProductRepository _productRepository;
    private readonly IBlobStorageService _blobStorageService;

    public RequestProductImageUploadCommandHandler(IProductRepository productRepository, IBlobStorageService blobStorageService)
    {
        _productRepository = productRepository;
        _blobStorageService = blobStorageService;
    }

    public async Task<ServiceResult<PresignedUploadUrl>> Handle(RequestProductImageUploadCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return ServiceResult<PresignedUploadUrl>.Failure("Product not found.");
        }

        var safeFileName = Path.GetFileName(request.FileName);
        var objectKey = $"products/{request.ProductId}/{Guid.NewGuid()}-{safeFileName}";

        var presignedUrl = await _blobStorageService.CreatePresignedUploadUrlAsync(objectKey, request.ContentType, cancellationToken);

        return ServiceResult<PresignedUploadUrl>.Success(presignedUrl);
    }
}
