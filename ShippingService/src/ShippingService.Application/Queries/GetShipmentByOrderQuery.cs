using ShippingService.Application.Common;
using ShippingService.Application.Dtos;
using MediatR;

namespace ShippingService.Application.Queries;

/// <summary>Ownership-checked against UserId.</summary>
public record GetShipmentByOrderQuery(Guid OrderId, Guid UserId) : IRequest<ServiceResult<ShipmentDto>>;
