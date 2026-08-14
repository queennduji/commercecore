using ShippingService.Application.Common;
using ShippingService.Application.Dtos;
using MediatR;

namespace ShippingService.Application.Queries;

/// <summary>Ownership-checked against UserId — same "not found rather than forbidden" pattern used
/// throughout the platform (OrderService's GetOrderQuery, PaymentService's GetPaymentQuery).</summary>
public record GetShipmentQuery(Guid ShipmentId, Guid UserId) : IRequest<ServiceResult<ShipmentDto>>;
