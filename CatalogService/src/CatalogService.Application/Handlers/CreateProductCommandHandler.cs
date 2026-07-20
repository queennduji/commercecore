using CatalogService.Application.Commands;
using CatalogService.Application.Common;
using CatalogService.Application.Dtos;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Mapping;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Events;
using MediatR;

namespace CatalogService.Application.Handlers;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ServiceResult<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IEventPublisher _eventPublisher;

    public CreateProductCommandHandler(IProductRepository productRepository, IEventPublisher eventPublisher)
    {
        _productRepository = productRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<ServiceResult<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Sku = request.Sku,
            Price = request.Price,
            Status = ProductStatus.Draft,
            CategoryId = request.CategoryId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _productRepository.AddAsync(product, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishProductCreatedAsync(new ProductCreatedEvent
        {
            ProductId = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            Price = product.Price,
            CategoryId = product.CategoryId,
            CreatedAt = product.CreatedAt
        }, cancellationToken);

        return ServiceResult<ProductDto>.Success(product.ToDto());
    }
}
