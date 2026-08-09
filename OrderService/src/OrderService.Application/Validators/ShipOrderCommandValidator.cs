using OrderService.Application.Commands;
using FluentValidation;

namespace OrderService.Application.Validators;

public class ShipOrderCommandValidator : AbstractValidator<ShipOrderCommand>
{
    public ShipOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}
