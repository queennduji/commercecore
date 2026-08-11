using PaymentService.Application.Commands;
using FluentValidation;

namespace PaymentService.Application.Validators;

public class RefundCommandValidator : AbstractValidator<RefundCommand>
{
    public RefundCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}
