using OrderService.Application.Common;
using OrderService.Application.Dtos;
using MediatR;

namespace OrderService.Application.Commands;

/// <summary>Customer action — ownership-checked against UserId. Actually charges via
/// PaymentService before flipping to Paid; fails the whole command if the charge is declined.</summary>
public record MarkOrderPaidCommand(Guid OrderId, Guid UserId, string PaymentMethodId) : IRequest<ServiceResult<OrderDto>>;
