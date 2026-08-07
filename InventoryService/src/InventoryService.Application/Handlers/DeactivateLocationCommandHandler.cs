using InventoryService.Application.Commands;
using InventoryService.Application.Common;
using InventoryService.Application.Interfaces;
using MediatR;

namespace InventoryService.Application.Handlers;

public class DeactivateLocationCommandHandler : IRequestHandler<DeactivateLocationCommand, ServiceResult<bool>>
{
    private readonly ILocationRepository _locationRepository;
    private readonly IInventoryItemRepository _inventoryItemRepository;

    public DeactivateLocationCommandHandler(ILocationRepository locationRepository, IInventoryItemRepository inventoryItemRepository)
    {
        _locationRepository = locationRepository;
        _inventoryItemRepository = inventoryItemRepository;
    }

    public async Task<ServiceResult<bool>> Handle(DeactivateLocationCommand request, CancellationToken cancellationToken)
    {
        var location = await _locationRepository.GetByIdAsync(request.Id, cancellationToken);
        if (location is null)
        {
            return ServiceResult<bool>.Failure("Location not found.");
        }

        if (await _inventoryItemRepository.AnyStockAtLocationAsync(request.Id, cancellationToken))
        {
            return ServiceResult<bool>.Failure("Cannot deactivate a location that still holds on-hand or reserved stock.");
        }

        location.IsActive = false;
        location.UpdatedAt = DateTime.UtcNow;

        await _locationRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }
}
