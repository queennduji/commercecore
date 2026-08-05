using CatalogService.Application.Commands;
using CatalogService.Application.Common;
using CatalogService.Application.Dtos;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Mapping;
using CatalogService.Domain.Events;
using MediatR;

namespace CatalogService.Application.Handlers;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ServiceResult<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductImageRepository _productImageRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICacheService _cacheService;

    public UpdateProductCommandHandler(
        IProductRepository productRepository,
        IProductImageRepository productImageRepository,
        IEventPublisher eventPublisher,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
        _eventPublisher = eventPublisher;
        _cacheService = cacheService;
    }

    public async Task<ServiceResult<ProductDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Failure("Product not found.");
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Status = request.Status;
        product.CategoryId = request.CategoryId;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishProductUpdatedAsync(new ProductUpdatedEvent
        {
            ProductId = product.Id,
            Name = product.Name,
            Price = product.Price,
            Status = product.Status.ToString(),
            CategoryId = product.CategoryId,
            UpdatedAt = product.UpdatedAt
        }, cancellationToken);

        var images = await _productImageRepository.ListByProductIdsAsync([product.Id], cancellationToken);
        var imageDtos = images.Select(i => i.ToDto()).ToList();

        await _cacheService.RemoveAsync(CacheKeys.Product(product.Id), cancellationToken);
        await _cacheService.RemoveByPrefixAsync(CacheKeys.ProductListPrefix, cancellationToken);

        return ServiceResult<ProductDto>.Success(product.ToDto(imageDtos));
    }
}
