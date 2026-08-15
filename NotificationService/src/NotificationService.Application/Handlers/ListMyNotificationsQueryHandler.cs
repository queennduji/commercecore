using NotificationService.Application.Common;
using NotificationService.Application.Dtos;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Mapping;
using NotificationService.Application.Queries;
using MediatR;

namespace NotificationService.Application.Handlers;

public class ListMyNotificationsQueryHandler : IRequestHandler<ListMyNotificationsQuery, ServiceResult<IReadOnlyList<NotificationDto>>>
{
    private readonly INotificationRepository _notificationRepository;

    public ListMyNotificationsQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<NotificationDto>>> Handle(ListMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var notifications = await _notificationRepository.ListByUserIdAsync(request.UserId, request.Page, request.PageSize, cancellationToken);
        return ServiceResult<IReadOnlyList<NotificationDto>>.Success(notifications.Select(n => n.ToDto()).ToList());
    }
}
