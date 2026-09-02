using OrderService.Application.Common;
using OrderService.Application.Dtos;
using MediatR;

namespace OrderService.Application.Commands;

/// <summary>Ops action – used to be reachable via an authenticated-caller HTTP endpoint with no
/// ownership check; now only ever dispatched internally by ShipmentDispatchedConsumer, so there's
/// no HTTP-level authorization question left to answer here (see OrdersController). Commits every
/// line's stock reservation, since this is the point the stock actually leaves the building.</summary>
public record ShipOrderCommand(Guid OrderId) : IRequest<ServiceResult<OrderDto>>;
