using ShippingService.Application.Common;
using ShippingService.Application.Dtos;
using MediatR;

namespace ShippingService.Application.Commands;

/// <summary>Ops action (any authenticated caller — no ownership check, no role system exists yet
/// to gate this to fulfillment staff specifically, mirrors OrderService's former Ship action).
/// TrackingCode is caller-supplied rather than generated here — in this simulated-label setup that
/// means one of EasyPost's own test tracking codes (EZ1000000001 etc), the same "caller picks the
/// test scenario" pattern as PaymentService's paymentMethodId.</summary>
public record DispatchShipmentCommand(Guid ShipmentId, string Carrier, string TrackingCode) : IRequest<ServiceResult<ShipmentDto>>;
