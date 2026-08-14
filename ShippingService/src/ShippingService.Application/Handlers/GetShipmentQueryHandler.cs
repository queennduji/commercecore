using ShippingService.Application.Common;
using ShippingService.Application.Dtos;
using ShippingService.Application.Interfaces;
using ShippingService.Application.Mapping;
using ShippingService.Application.Queries;
using MediatR;

namespace ShippingService.Application.Handlers;

public class GetShipmentQueryHandler : IRequestHandler<GetShipmentQuery, ServiceResult<ShipmentDto>>
{
    private readonly IShipmentRepository _shipmentRepository;

    public GetShipmentQueryHandler(IShipmentRepository shipmentRepository)
    {
        _shipmentRepository = shipmentRepository;
    }

    public async Task<ServiceResult<ShipmentDto>> Handle(GetShipmentQuery request, CancellationToken cancellationToken)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(request.ShipmentId, cancellationToken);
        return shipment is null || shipment.UserId != request.UserId
            ? ServiceResult<ShipmentDto>.Failure("Shipment not found.")
            : ServiceResult<ShipmentDto>.Success(shipment.ToDto());
    }
}
