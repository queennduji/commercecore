using InventoryService.Application.Commands;
using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Mapping;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Events;
using MediatR;

namespace InventoryService.Application.Handlers;

public class ReleaseReservationCommandHandler : IRequestHandler<ReleaseReservationCommand, ServiceResult<StockReservationDto>>
{
    private readonly IStockReservationRepository _stockReservationRepository;
    private readonly IInventoryItemRepository _inventoryItemRepository;
    private readonly IEventPublisher _eventPublisher;

    public ReleaseReservationCommandHandler(
        IStockReservationRepository stockReservationRepository,
        IInventoryItemRepository inventoryItemRepository,
        IEventPublisher eventPublisher)
    {
        _stockReservationRepository = stockReservationRepository;
        _inventoryItemRepository = inventoryItemRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<ServiceResult<StockReservationDto>> Handle(ReleaseReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await _stockReservationRepository.GetByIdAsync(request.ReservationId, cancellationToken);
        if (reservation is null)
        {
            return ServiceResult<StockReservationDto>.Failure("Reservation not found.");
        }

        if (reservation.Status != ReservationStatus.Active)
        {
            return ServiceResult<StockReservationDto>.Failure($"Reservation is already {reservation.Status}.");
        }

        var item = await _inventoryItemRepository.GetByProductAndLocationAsync(reservation.ProductId, reservation.LocationId, cancellationToken);
        var now = DateTime.UtcNow;

        if (item is not null)
        {
            item.Reserved = Math.Max(0, item.Reserved - reservation.Quantity);
            item.UpdatedAt = now;
            await _inventoryItemRepository.SaveChangesAsync(cancellationToken);
        }

        reservation.Status = ReservationStatus.Released;
        reservation.UpdatedAt = now;
        await _stockReservationRepository.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishReservationReleasedAsync(new ReservationReleasedEvent
        {
            ReservationId = reservation.Id,
            ProductId = reservation.ProductId,
            LocationId = reservation.LocationId,
            Quantity = reservation.Quantity,
            ReleasedAt = now
        }, cancellationToken);

        return ServiceResult<StockReservationDto>.Success(reservation.ToDto());
    }
}
