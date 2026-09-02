using ShippingService.Application.Common;
using ShippingService.Application.Dtos;
using MediatR;

namespace ShippingService.Application.Commands;

/// <summary>Ops action – re-polls EasyPost for the tracker's latest status and reflects it locally.
/// There is no public webhook endpoint in this local-dev setup for EasyPost to push updates to, so
/// this pull-based refresh is how the (real, test-mode) tracking status actually gets in. Publishes
/// ShipmentDeliveredEvent/ShipmentExceptionEvent only on a genuine transition into that state, so
/// calling this repeatedly after the shipment is already Delivered is a harmless no-op.</summary>
public record RefreshShipmentTrackingCommand(Guid ShipmentId) : IRequest<ServiceResult<ShipmentDto>>;
