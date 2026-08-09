using OrderService.Application.Common;
using OrderService.Application.Dtos;
using MediatR;

namespace OrderService.Application.Commands;

/// <summary>Ops action (see ShipOrderCommand).</summary>
public record DeliverOrderCommand(Guid OrderId) : IRequest<ServiceResult<OrderDto>>;
