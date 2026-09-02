using NotificationService.Application.Commands;
using NotificationService.Application.Common;
using NotificationService.Application.Dtos;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Mapping;
using NotificationService.Application.Templating;
using NotificationService.Domain.Entities;
using MediatR;

namespace NotificationService.Application.Handlers;

public class SendOrderLifecycleNotificationCommandHandler : IRequestHandler<SendOrderLifecycleNotificationCommand, ServiceResult<NotificationDto>>
{
    private readonly IUserContactRepository _userContactRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmailGateway _emailGateway;
    private readonly ISmsGateway _smsGateway;

    public SendOrderLifecycleNotificationCommandHandler(
        IUserContactRepository userContactRepository,
        INotificationRepository notificationRepository,
        IEmailGateway emailGateway,
        ISmsGateway smsGateway)
    {
        _userContactRepository = userContactRepository;
        _notificationRepository = notificationRepository;
        _emailGateway = emailGateway;
        _smsGateway = smsGateway;
    }

    public async Task<ServiceResult<NotificationDto>> Handle(SendOrderLifecycleNotificationCommand request, CancellationToken cancellationToken)
    {
        var contact = await _userContactRepository.GetContactAsync(request.UserId, cancellationToken);
        var now = DateTime.UtcNow;

        if (contact is null || (contact.Email is null && contact.PhoneNumber is null))
        {
            // No known contact for this user on any channel (e.g. auth.user-registered.v1 hasn't
            // been consumed yet, or arrived after this event somehow) – still recorded for audit,
            // same record-every-attempt reasoning as Payment/Shipment. Channel is a nominal
            // Email placeholder since there's no real channel to attribute this attempt to.
            var (missingSubject, missingBody) = NotificationTemplate.Render(request.Type, request.OrderId, request.Detail);
            var missing = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Channel = NotificationChannel.Email,
                Recipient = string.Empty,
                Type = request.Type,
                Subject = missingSubject,
                Body = missingBody,
                Status = NotificationStatus.Failed,
                FailureReason = "No known contact information for this user.",
                CreatedAt = now
            };
            await _notificationRepository.AddAsync(missing, cancellationToken);
            await _notificationRepository.SaveChangesAsync(cancellationToken);
            return ServiceResult<NotificationDto>.Failure("No known contact information for this user.");
        }

        // A user with both an email and a phone on file gets two Notification rows for the same
        // event – one per channel – rather than one row trying to describe two outcomes. The
        // command still returns a single DTO (whichever channel succeeded, preferring the first
        // one tried); the full picture across channels is what GET /api/notifications/me is for.
        NotificationDto? succeededDto = null;
        var errors = new List<string>();

        if (contact.Email is not null)
        {
            var (subject, body) = NotificationTemplate.Render(request.Type, request.OrderId, request.Detail);
            var gatewayResult = await _emailGateway.SendAsync(contact.Email, subject, body, cancellationToken);

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Channel = NotificationChannel.Email,
                Recipient = contact.Email,
                Type = request.Type,
                Subject = subject,
                Body = body,
                Status = gatewayResult.Succeeded ? NotificationStatus.Sent : NotificationStatus.Failed,
                ProviderMessageId = gatewayResult.ProviderMessageId,
                FailureReason = gatewayResult.FailureReason,
                CreatedAt = now
            };
            await _notificationRepository.AddAsync(notification, cancellationToken);

            if (gatewayResult.Succeeded)
            {
                succeededDto ??= notification.ToDto();
            }
            else
            {
                errors.Add(gatewayResult.FailureReason ?? "Failed to send email notification.");
            }
        }

        if (contact.PhoneNumber is not null)
        {
            var smsBody = NotificationTemplate.RenderSms(request.Type, request.OrderId, request.Detail);
            var gatewayResult = await _smsGateway.SendAsync(contact.PhoneNumber, smsBody, cancellationToken);

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Channel = NotificationChannel.Sms,
                Recipient = contact.PhoneNumber,
                Type = request.Type,
                Subject = string.Empty,
                Body = smsBody,
                Status = gatewayResult.Succeeded ? NotificationStatus.Sent : NotificationStatus.Failed,
                ProviderMessageId = gatewayResult.ProviderMessageId,
                FailureReason = gatewayResult.FailureReason,
                CreatedAt = now
            };
            await _notificationRepository.AddAsync(notification, cancellationToken);

            if (gatewayResult.Succeeded)
            {
                succeededDto ??= notification.ToDto();
            }
            else
            {
                errors.Add(gatewayResult.FailureReason ?? "Failed to send SMS notification.");
            }
        }

        await _notificationRepository.SaveChangesAsync(cancellationToken);

        return succeededDto is not null
            ? ServiceResult<NotificationDto>.Success(succeededDto)
            : ServiceResult<NotificationDto>.Failure(errors.ToArray());
    }
}
