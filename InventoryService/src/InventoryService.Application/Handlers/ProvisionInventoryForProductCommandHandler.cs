using InventoryService.Application.Commands;
using InventoryService.Application.Common;
using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using MediatR;

namespace InventoryService.Application.Handlers;

public class ProvisionInventoryForProductCommandHandler : IRequestHandler<ProvisionInventoryForProductCommand, ServiceResult<bool>>
{
    private readonly ILocationRepository _locationRepository;
    private readonly IInventoryItemRepository _inventoryItemRepository;

    public ProvisionInventoryForProductCommandHandler(
        ILocationRepository locationRepository,
        IInventoryItemRepository inventoryItemRepository)
    {
        _locationRepository = locationRepository;
        _inventoryItemRepository = inventoryItemRepository;
    }

    public async Task<ServiceResult<bool>> Handle(ProvisionInventoryForProductCommand request, CancellationToken cancellationToken)
    {
        var activeLocations = await _locationRepository.ListActiveAsync(cancellationToken);
        if (activeLocations.Count == 0)
        {
            return ServiceResult<bool>.Success(true);
        }

        var now = DateTime.UtcNow;
        var created = false;

        foreach (var location in activeLocations)
        {
            var existing = await _inventoryItemRepository.GetByProductAndLocationAsync(request.ProductId, location.Id, cancellationToken);
            if (existing is not null)
            {
                continue;
            }

            await _inventoryItemRepository.AddAsync(new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                LocationId = location.Id,
                OnHand = 0,
                Reserved = 0,
                CreatedAt = now,
                UpdatedAt = now
            }, cancellationToken);
            created = true;
        }

        if (created)
        {
            await _inventoryItemRepository.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult<bool>.Success(true);
    }
}
