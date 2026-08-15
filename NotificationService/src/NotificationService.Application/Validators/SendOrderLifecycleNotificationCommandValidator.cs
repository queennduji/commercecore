using NotificationService.Application.Commands;
using FluentValidation;

namespace NotificationService.Application.Validators;

public class SendOrderLifecycleNotificationCommandValidator : AbstractValidator<SendOrderLifecycleNotificationCommand>
{
    public SendOrderLifecycleNotificationCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
