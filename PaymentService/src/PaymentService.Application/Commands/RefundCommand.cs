using PaymentService.Application.Common;
using PaymentService.Application.Dtos;
using MediatR;

namespace PaymentService.Application.Commands;

/// <summary>Called by OrderService (an ops action, no ownership check on that side) when an order
/// moves to Refunded. Refunds the most recent Succeeded payment on record for the order.</summary>
public record RefundCommand(Guid OrderId) : IRequest<ServiceResult<PaymentDto>>;
