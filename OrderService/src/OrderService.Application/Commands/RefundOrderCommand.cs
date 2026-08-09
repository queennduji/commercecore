using OrderService.Application.Common;
using OrderService.Application.Dtos;
using MediatR;

namespace OrderService.Application.Commands;

/// <summary>Ops action (see ShipOrderCommand). Valid from Paid/Shipped/Delivered — deliberately
/// does not touch inventory (restocking a post-shipment return is out of scope for now).</summary>
public record RefundOrderCommand(Guid OrderId) : IRequest<ServiceResult<OrderDto>>;
