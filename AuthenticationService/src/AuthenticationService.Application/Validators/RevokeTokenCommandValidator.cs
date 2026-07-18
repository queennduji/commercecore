using AuthenticationService.Application.Commands;
using FluentValidation;

namespace AuthenticationService.Application.Validators;

public class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
