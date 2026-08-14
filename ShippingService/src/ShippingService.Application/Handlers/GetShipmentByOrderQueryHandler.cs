using ShippingService.Application.Common;
using ShippingService.Application.Dtos;
using ShippingService.Application.Interfaces;
using ShippingService.Application.Mapping;
using ShippingService.Application.Queries;
using MediatR;

namespace ShippingService.Application.Handlers;

public class GetShipmentByOrderQueryHandler : IRequestHandler<GetShipmentByOrderQuery, ServiceResult<ShipmentDto>>
{
    private readonly IShipmentRepository _shipmentRepository;

    public GetShipmentByOrderQueryHandler(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<ServiceResult<ShipmentDto>> Handle(GetShipmentByOrderQuery request, CancellationToken cancellationToken)
    {
        var shipment = await _shipmentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        return shipment is null || shipment.UserId != request.UserId
            ? ServiceResult<ShipmentDto>.Failure("Shipment not found.")
            : ServiceResult<ShipmentDto>.Success(shipment.ToDto());
    }
}
