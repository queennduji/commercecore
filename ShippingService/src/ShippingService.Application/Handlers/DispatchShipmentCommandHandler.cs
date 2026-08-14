using ShippingService.Application.Commands;
using ShippingService.Application.Common;
using ShippingService.Application.Dtos;
using ShippingService.Application.Interfaces;
using ShippingService.Application.Mapping;
using ShippingService.Domain.Entities;
using ShippingService.Domain.Events;
using MediatR;

namespace ShippingService.Application.Handlers;

public class DispatchShipmentCommandHandler : IRequestHandler<DispatchShipmentCommand, ServiceResult<ShipmentDto>>
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IShippingCarrierGateway _carrierGateway;
    private readonly IEventPublisher _eventPublisher;

    public DispatchShipmentCommandHandler(
        IShipmentRepository shipmentRepository,
        IShippingCarrierGateway carrierGateway,
        IEventPublisher eventPublisher)
    {
        _shipmentRepository = shipmentRepository;
        _carrierGateway = carrierGateway;
        _eventPublisher = eventPublisher;
    }

    public async Task<ServiceResult<ShipmentDto>> Handle(DispatchShipmentCommand request, CancellationToken cancellationToken)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);
        if (shipment is null)
        {
            return ServiceResult<ShipmentDto>.Failure("Shipment not found.");
        }

        if (shipment.Status != ShipmentStatus.AwaitingFulfillment)
        {
            return ServiceResult<ShipmentDto>.Failure($"Cannot dispatch a shipment from status {shipment.Status}.");
        }

        var gatewayResult = await _carrierGateway.CreateTrackerAsync(request.TrackingCode, request.Carrier, cancellationToken);
        if (!gatewayResult.Succeeded)
        {
            return ServiceResult<ShipmentDto>.Failure(gatewayResult.FailureReason ?? "Failed to create carrier tracker.");
        }

        var now = DateTime.UtcNow;
        shipment.CarrierName = request.Carrier;
        shipment.TrackingNumber = request.TrackingCode;
        shipment.ProviderTrackerId = gatewayResult.ProviderTrackerId;
        shipment.Status = ShipmentStatus.Dispatched;
        shipment.DispatchedAt = now;
        shipment.UpdatedAt = now;
        await _shipmentRepository.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishShipmentDispatchedAsync(new ShipmentDispatchedEvent
        {
            ShipmentId = shipment.Id,
            OrderId = shipment.OrderId,
            UserId = shipment.UserId,
            CarrierName = shipment.CarrierName,
            TrackingNumber = shipment.TrackingNumber,
            DispatchedAt = now
        }, cancellationToken);

        return ServiceResult<ShipmentDto>.Success(shipment.ToDto());
    }
}
