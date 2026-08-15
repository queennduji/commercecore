using NotificationService.Application.Common;
using NotificationService.Application.Dtos;
using MediatR;

namespace NotificationService.Application.Queries;

/// <summary>Ownership-checked against UserId — same "not found rather than forbidden" pattern
/// used throughout the platform.</summary>
public record GetNotificationQuery(Guid NotificationId, Guid UserId) : IRequest<ServiceResult<NotificationDto>>;
