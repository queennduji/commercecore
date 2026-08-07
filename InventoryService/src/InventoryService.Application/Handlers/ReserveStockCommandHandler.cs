using InventoryService.Application.Commands;
using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Mapping;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Events;
using MediatR;

namespace InventoryService.Application.Handlers;

public class ReserveStockCommandHandler : IRequestHandler<ReserveStockCommand, ServiceResult<StockReservationDto>>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;
    private readonly IStockReservationRepository _stockReservationRepository;
    private readonly IEventPublisher _eventPublisher;

    public ReserveStockCommandHandler(
        IInventoryItemRepository inventoryItemRepository,
        IStockReservationRepository stockReservationRepository,
        IEventPublisher eventPublisher)
    {
        _inventoryItemRepository = inventoryItemRepository;
        _stockReservationRepository = stockReservationRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<ServiceResult<StockReservationDto>> Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        var item = await _inventoryItemRepository.GetByProductAndLocationAsync(request.ProductId, request.LocationId, cancellationToken);
        if (item is null || item.Available < request.Quantity)
        {
            return ServiceResult<StockReservationDto>.Failure("Insufficient available stock at this location.");
        }

        var now = DateTime.UtcNow;

        item.Reserved += request.Quantity;
        item.UpdatedAt = now;

        var reservation = new StockReservation
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            LocationId = request.LocationId,
            Quantity = request.Quantity,
            Status = ReservationStatus.Active,
            ReferenceId = request.ReferenceId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _stockReservationRepository.AddAsync(reservation, cancellationToken);
        await _inventoryItemRepository.SaveChangesAsync(cancellationToken);
        await _stockReservationRepository.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishStockReservedAsync(new StockReservedEvent
        {
            ReservationId = reservation.Id,
            ProductId = request.ProductId,
            LocationId = request.LocationId,
            Quantity = request.Quantity,
            ReferenceId = request.ReferenceId,
            ReservedAt = now
        }, cancellationToken);

        return ServiceResult<StockReservationDto>.Success(reservation.ToDto());
    }
}
