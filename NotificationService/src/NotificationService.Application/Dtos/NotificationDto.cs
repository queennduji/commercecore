namespace NotificationService.Application.Dtos;

public record NotificationDto(
    Guid Id,
    Guid UserId,
    string Channel,
    string Recipient,
    string Type,
    string Subject,
    string Body,
    string Status,
    string? ProviderMessageId,
    string? FailureReason,
    DateTime CreatedAt);
