using ShippingService.Application.Common;
using ShippingService.Application.Dtos;
using MediatR;

namespace ShippingService.Application.Commands;

/// <summary>Internal — dispatched only by OrderPaidConsumer when order.paid.v1 arrives, never
/// exposed via HTTP. Idempotent: Kafka's at-least-once delivery means this can be dispatched more
/// than once for the same order, so the handler treats an existing shipment for OrderId as success
/// rather than erroring.</summary>
public record CreateShipmentForOrderCommand(Guid OrderId, Guid UserId, string ShippingAddress) : IRequest<ServiceResult<ShipmentDto>>;
