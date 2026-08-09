using OrderService.Application.Commands;
using FluentValidation;

namespace OrderService.Application.Validators;

public class RefundOrderCommandValidator : AbstractValidator<RefundOrderCommand>
{
    public RefundOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}
