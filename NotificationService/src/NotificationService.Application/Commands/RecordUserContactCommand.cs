using NotificationService.Application.Common;
using MediatR;

namespace NotificationService.Application.Commands;

/// <summary>Internal – dispatched only by UserRegisteredConsumer when auth.user-registered.v1
/// arrives, never exposed via HTTP. Idempotent upsert, since Kafka's at-least-once delivery can
/// dispatch this more than once for the same user.</summary>
public record RecordUserContactCommand(Guid UserId, string Email, string? PhoneNumber = null) : IRequest<ServiceResult<bool>>;
