using OrderService.Application.Commands;
using FluentValidation;

namespace OrderService.Application.Validators;

public class MarkOrderPaidCommandValidator : AbstractValidator<MarkOrderPaidCommand>
{
    public MarkOrderPaidCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PaymentMethodId).NotEmpty();
    }
}
