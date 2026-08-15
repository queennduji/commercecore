using NotificationService.Application.Common;
using NotificationService.Application.Dtos;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Mapping;
using NotificationService.Application.Queries;
using MediatR;

namespace NotificationService.Application.Handlers;

public class GetNotificationQueryHandler : IRequestHandler<GetNotificationQuery, ServiceResult<NotificationDto>>
{
    private readonly INotificationRepository _notificationRepository;

    public GetNotificationQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<ServiceResult<NotificationDto>> Handle(GetNotificationQuery request, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
        return notification is null || notification.UserId != request.UserId
            ? ServiceResult<NotificationDto>.Failure("Notification not found.")
            : ServiceResult<NotificationDto>.Success(notification.ToDto());
    }
}
