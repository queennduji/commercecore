using PaymentService.Application.Common;
using PaymentService.Application.Dtos;
using MediatR;

namespace PaymentService.Application.Commands;

/// <summary>Called by OrderService (forwarding the customer's own JWT) when the customer hits
/// /api/orders/{id}/pay. Records a Payment either way — Succeeded or Failed — so there's always
/// an audit trail of the attempt, not just of successful charges.</summary>
public record ChargeCommand(
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string Currency,
    string PaymentMethodId) : IRequest<ServiceResult<PaymentDto>>;
