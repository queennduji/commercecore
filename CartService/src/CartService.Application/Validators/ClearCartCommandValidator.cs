using CartService.Application.Commands;
using FluentValidation;

namespace CartService.Application.Validators;

public class ClearCartCommandValidator : AbstractValidator<ClearCartCommand>
{
    public ClearCartCommandValidator()
    {
        RuleFor(x => x.CartId).NotEmpty();
    }
}
