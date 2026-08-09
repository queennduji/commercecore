using OrderService.Application.Commands;
using FluentValidation;

namespace OrderService.Application.Validators;

public class DeliverOrderCommandValidator : AbstractValidator<DeliverOrderCommand>
{
    public DeliverOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}
