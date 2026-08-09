using OrderService.Application.Common;
using OrderService.Application.Dtos;
using MediatR;

namespace OrderService.Application.Commands;

/// <summary>Customer action — ownership-checked against UserId. Stands in for a real payment
/// confirmation until a Payment service exists to drive this transition instead.</summary>
public record MarkOrderPaidCommand(Guid OrderId, Guid UserId) : IRequest<ServiceResult<OrderDto>>;
