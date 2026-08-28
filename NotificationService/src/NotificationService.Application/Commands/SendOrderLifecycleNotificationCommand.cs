using NotificationService.Application.Common;
using NotificationService.Application.Dtos;
using NotificationService.Domain.Entities;
using MediatR;

namespace NotificationService.Application.Commands;

/// <summary>Internal — dispatched only by the seven order/payment lifecycle consumers, never
/// exposed via HTTP (UserRegisteredConsumer is the eighth consumer in this service, but it
/// dispatches RecordUserContactCommand instead). Detail carries optional extra context for the
/// message body (currently only used by PaymentFailed, to include the decline reason).</summary>
public record SendOrderLifecycleNotificationCommand(
    Guid OrderId,
    Guid UserId,
    NotificationType Type,
    string? Detail = null) : IRequest<ServiceResult<NotificationDto>>;
