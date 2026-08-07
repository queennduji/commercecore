using InventoryService.Application.Commands;
using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Mapping;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Events;
using MediatR;

namespace InventoryService.Application.Handlers;

public class CommitReservationCommandHandler : IRequestHandler<CommitReservationCommand, ServiceResult<StockReservationDto>>
{
    private readonly IStockReservationRepository _stockReservationRepository;
    private readonly IInventoryItemRepository _inventoryItemRepository;
    private readonly IEventPublisher _eventPublisher;

    public CommitReservationCommandHandler(
        IStockReservationRepository stockReservationRepository,
        IInventoryItemRepository inventoryItemRepository,
        IEventPublisher eventPublisher)
    {
        _stockReservationRepository = stockReservationRepository;
        _inventoryItemRepository = inventoryItemRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<ServiceResult<StockReservationDto>> Handle(CommitReservationCommand request, CancellationToken cancellationToken)
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
            // Committing means the reserved stock actually leaves the building (e.g. the order
            // shipped): it comes off both the on-hand count and the reserved hold together.
            item.OnHand = Math.Max(0, item.OnHand - reservation.Quantity);
            item.Reserved = Math.Max(0, item.Reserved - reservation.Quantity);
            item.UpdatedAt = now;
            await _inventoryItemRepository.SaveChangesAsync(cancellationToken);
        }

        reservation.Status = ReservationStatus.Committed;
        reservation.UpdatedAt = now;
        await _stockReservationRepository.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishReservationCommittedAsync(new ReservationCommittedEvent
        {
            ReservationId = reservation.Id,
            ProductId = reservation.ProductId,
            LocationId = reservation.LocationId,
            Quantity = reservation.Quantity,
            CommittedAt = now
        }, cancellationToken);

        return ServiceResult<StockReservationDto>.Success(reservation.ToDto());
    }
}
