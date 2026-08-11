using PaymentService.Application.Commands;
using FluentValidation;

namespace PaymentService.Application.Validators;

public class ChargeCommandValidator : AbstractValidator<ChargeCommand>
{
    public ChargeCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.PaymentMethodId).NotEmpty();
    }
}
