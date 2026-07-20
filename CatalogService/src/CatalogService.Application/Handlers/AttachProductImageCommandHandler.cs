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

    public AttachProductImageCommandHandler(
        IProductRepository productRepository,
        IProductImageRepository productImageRepository,
        IBlobStorageService blobStorageService)
    {
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
        _blobStorageService = blobStorageService;
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

        return ServiceResult<ProductImageDto>.Success(image.ToDto());
    }
}
