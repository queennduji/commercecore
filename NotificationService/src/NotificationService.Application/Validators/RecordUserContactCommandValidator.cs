using NotificationService.Application.Commands;
using FluentValidation;

namespace NotificationService.Application.Validators;

public class RecordUserContactCommandValidator : AbstractValidator<RecordUserContactCommand>
{
    public RecordUserContactCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
