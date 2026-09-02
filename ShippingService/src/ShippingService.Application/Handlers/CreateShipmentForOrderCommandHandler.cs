using ShippingService.Application.Commands;
using ShippingService.Application.Common;
using ShippingService.Application.Dtos;
using ShippingService.Application.Interfaces;
using ShippingService.Application.Mapping;
using ShippingService.Domain.Entities;
using MediatR;

namespace ShippingService.Application.Handlers;

public class CreateShipmentForOrderCommandHandler : IRequestHandler<CreateShipmentForOrderCommand, ServiceResult<ShipmentDto>>
{
    private readonly IShipmentRepository _shipmentRepository;

    public CreateShipmentForOrderCommandHandler(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<ServiceResult<ShipmentDto>> Handle(CreateShipmentForOrderCommand request, CancellationToken cancellationToken)
    {
        var existing = await _shipmentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (existing is not null)
        {
            // Idempotent: Kafka's at-least-once delivery can dispatch this command more than once
            // for the same order – treat an existing shipment as success rather than violating the
            // one-shipment-per-order unique index.
            return ServiceResult<ShipmentDto>.Success(existing.ToDto());
        }

        var now = DateTime.UtcNow;
        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = request.OrderId,
            UserId = request.UserId,
            ShippingAddress = request.ShippingAddress,
            Status = ShipmentStatus.AwaitingFulfillment,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _shipmentRepository.AddAsync(shipment, cancellationToken);
        await _shipmentRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<ShipmentDto>.Success(shipment.ToDto());
    }
}
