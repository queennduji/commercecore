using ShippingService.Application.Commands;
using FluentValidation;

namespace ShippingService.Application.Validators;

public class CreateShipmentForOrderCommandValidator : AbstractValidator<CreateShipmentForOrderCommand>
{
    public CreateShipmentForOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ShippingAddress).NotEmpty();
    }
}
