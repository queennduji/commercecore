using ShippingService.Application.Commands;
using ShippingService.Application.Common;
using ShippingService.Application.Dtos;
using ShippingService.Application.Interfaces;
using ShippingService.Application.Mapping;
using ShippingService.Domain.Entities;
using ShippingService.Domain.Events;
using MediatR;

namespace ShippingService.Application.Handlers;

public class RefreshShipmentTrackingCommandHandler : IRequestHandler<RefreshShipmentTrackingCommand, ServiceResult<ShipmentDto>>
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IShippingCarrierGateway _carrierGateway;
    private readonly IEventPublisher _eventPublisher;

    public RefreshShipmentTrackingCommandHandler(
        IShipmentRepository shipmentRepository,
        IShippingCarrierGateway carrierGateway,
        IEventPublisher eventPublisher)
    {
        _shipmentRepository = shipmentRepository;
        _carrierGateway = carrierGateway;
        _eventPublisher = eventPublisher;
    }

    public async Task<ServiceResult<ShipmentDto>> Handle(RefreshShipmentTrackingCommand request, CancellationToken cancellationToken)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);
        if (shipment is null)
        {
            return ServiceResult<ShipmentDto>.Failure("Shipment not found.");
        }

        if (shipment.ProviderTrackerId is null)
        {
            return ServiceResult<ShipmentDto>.Failure("Shipment has not been dispatched yet.");
        }

        var gatewayResult = await _carrierGateway.RetrieveTrackerAsync(shipment.ProviderTrackerId, cancellationToken);
        if (!gatewayResult.Succeeded)
        {
            return ServiceResult<ShipmentDto>.Failure(gatewayResult.FailureReason ?? "Failed to refresh tracking.");
        }

        var mappedStatus = MapCarrierStatus(gatewayResult.CarrierStatus);
        if (mappedStatus is null || mappedStatus == shipment.Status)
        {
            // Either an unrecognized/"unknown" carrier status (nothing to reflect) or no actual
            // transition – either way, not an error, and no duplicate event to publish.
            return ServiceResult<ShipmentDto>.Success(shipment.ToDto());
        }

        var now = DateTime.UtcNow;
        shipment.Status = mappedStatus.Value;
        shipment.UpdatedAt = now;

        if (mappedStatus == ShipmentStatus.Delivered)
        {
            shipment.DeliveredAt = now;
            await _shipmentRepository.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishShipmentDeliveredAsync(new ShipmentDeliveredEvent
            {
                ShipmentId = shipment.Id,
                OrderId = shipment.OrderId,
                UserId = shipment.UserId,
                DeliveredAt = now
            }, cancellationToken);
        }
        else if (mappedStatus == ShipmentStatus.Exception)
        {
            shipment.ExceptionReason = gatewayResult.CarrierStatus;
            await _shipmentRepository.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishShipmentExceptionAsync(new ShipmentExceptionEvent
            {
                ShipmentId = shipment.Id,
                OrderId = shipment.OrderId,
                UserId = shipment.UserId,
                Reason = gatewayResult.CarrierStatus ?? "Unknown carrier exception.",
                OccurredAt = now
            }, cancellationToken);
        }
        else
        {
            await _shipmentRepository.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult<ShipmentDto>.Success(shipment.ToDto());
    }

    private static ShipmentStatus? MapCarrierStatus(string? carrierStatus) => carrierStatus switch
    {
        "pre_transit" => ShipmentStatus.Dispatched,
        "in_transit" or "out_for_delivery" or "available_for_pickup" => ShipmentStatus.InTransit,
        "delivered" => ShipmentStatus.Delivered,
        "return_to_sender" or "failure" or "cancelled" or "error" => ShipmentStatus.Exception,
        _ => null
    };
}
