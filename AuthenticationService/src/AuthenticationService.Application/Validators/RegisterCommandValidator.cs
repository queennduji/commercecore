using AuthenticationService.Application.Commands;
using FluentValidation;

namespace AuthenticationService.Application.Validators;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+[1-9]\d{7,14}$")
            .When(x => x.PhoneNumber is not null)
            .WithMessage("PhoneNumber must be in E.164 format (e.g. +15551234567).");
    }
}
