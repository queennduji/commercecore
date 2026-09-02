using OrderService.Application.Common;
using OrderService.Application.Dtos;
using MediatR;

namespace OrderService.Application.Commands;

/// <summary>Customer action – ownership-checked against UserId. Only valid from Pending/Paid
/// (pre-shipment); releases every line's stock reservation.</summary>
public record CancelOrderCommand(Guid OrderId, Guid UserId) : IRequest<ServiceResult<OrderDto>>;
