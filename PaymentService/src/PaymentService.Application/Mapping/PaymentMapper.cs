using PaymentService.Application.Dtos;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Mapping;

public static class PaymentMapper
{
    public static PaymentDto ToDto(this Payment payment) => new(
        payment.Id,
        payment.OrderId,
        payment.UserId,
        payment.Amount,
        payment.Currency,
        payment.Status.ToString(),
        payment.ProviderReference,
        payment.FailureReason,
        payment.CreatedAt,
        payment.UpdatedAt);
}
