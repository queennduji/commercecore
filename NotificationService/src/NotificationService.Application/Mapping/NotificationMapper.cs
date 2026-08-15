using NotificationService.Application.Dtos;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Mapping;

public static class NotificationMapper
{
    public static NotificationDto ToDto(this Notification notification) => new(
        notification.Id,
        notification.UserId,
        notification.Channel.ToString(),
        notification.Recipient,
        notification.Type.ToString(),
        notification.Subject,
        notification.Body,
        notification.Status.ToString(),
        notification.ProviderMessageId,
        notification.FailureReason,
        notification.CreatedAt);
}
