namespace PaymentService.Application.Dtos;

public record PaymentDto(
    Guid Id,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string Currency,
    string Status,
    string? ProviderReference,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime UpdatedAt);
