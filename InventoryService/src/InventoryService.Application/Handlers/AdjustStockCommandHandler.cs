using InventoryService.Application.Commands;
using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Mapping;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Events;
using MediatR;

namespace InventoryService.Application.Handlers;

public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, ServiceResult<InventoryItemDto>>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IEventPublisher _eventPublisher;

    public AdjustStockCommandHandler(
        IInventoryItemRepository inventoryItemRepository,
        ILocationRepository locationRepository,
        IEventPublisher eventPublisher)
    {
        _inventoryItemRepository = inventoryItemRepository;
        _locationRepository = locationRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<ServiceResult<InventoryItemDto>> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var location = await _locationRepository.GetByIdAsync(request.LocationId, cancellationToken);
        if (location is null)
        {
            return ServiceResult<InventoryItemDto>.Failure("Location not found.");
        }

        var item = await _inventoryItemRepository.GetByProductAndLocationAsync(request.ProductId, request.LocationId, cancellationToken);
        var now = DateTime.UtcNow;

        if (item is null)
        {
            item = new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                LocationId = request.LocationId,
                OnHand = 0,
                Reserved = 0,
                CreatedAt = now,
                UpdatedAt = now
            };

            var startingOnHand = item.OnHand + request.Delta;
            if (startingOnHand < 0)
            {
                return ServiceResult<InventoryItemDto>.Failure("Adjustment would result in negative stock.");
            }

            item.OnHand = startingOnHand;
            item.UpdatedAt = now;
            await _inventoryItemRepository.AddAsync(item, cancellationToken);
        }
        else
        {
            var newOnHand = item.OnHand + request.Delta;
            if (newOnHand < 0)
            {
                return ServiceResult<InventoryItemDto>.Failure("Adjustment would result in negative stock.");
            }

            item.OnHand = newOnHand;
            item.UpdatedAt = now;
        }

        await _inventoryItemRepository.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishStockAdjustedAsync(new StockAdjustedEvent
        {
            ProductId = request.ProductId,
            LocationId = request.LocationId,
            Delta = request.Delta,
            OnHandAfter = item.OnHand,
            Reason = request.Reason,
            AdjustedAt = now
        }, cancellationToken);

        return ServiceResult<InventoryItemDto>.Success(item.ToDto());
    }
}
