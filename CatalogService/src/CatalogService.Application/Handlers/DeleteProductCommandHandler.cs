using CatalogService.Application.Commands;
using CatalogService.Application.Common;
using CatalogService.Application.Interfaces;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Events;
using MediatR;

namespace CatalogService.Application.Handlers;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, ServiceResult<bool>>
{
    private readonly IProductRepository _productRepository;
    private readonly IEventPublisher _eventPublisher;

    public DeleteProductCommandHandler(IProductRepository productRepository, IEventPublisher eventPublisher)
    {
        _productRepository = productRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<ServiceResult<bool>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return ServiceResult<bool>.Failure("Product not found.");
        }

        product.Status = ProductStatus.Archived;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishProductDeletedAsync(new ProductDeletedEvent
        {
            ProductId = product.Id,
            DeletedAt = product.UpdatedAt
        }, cancellationToken);

        return ServiceResult<bool>.Success(true);
    }
}
