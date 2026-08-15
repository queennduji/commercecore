using NotificationService.Application.Common;
using NotificationService.Application.Dtos;
using MediatR;

namespace NotificationService.Application.Queries;

/// <summary>Always the caller's own — there's no "list any user's notifications" ops variant,
/// unlike Order/Payment/Shipment which have ops actions standing in for back-office staff.</summary>
public record ListMyNotificationsQuery(Guid UserId, int Page, int PageSize) : IRequest<ServiceResult<IReadOnlyList<NotificationDto>>>;
