using ShippingService.Application.Commands;
using FluentValidation;

namespace ShippingService.Application.Validators;

public class DispatchShipmentCommandValidator : AbstractValidator<DispatchShipmentCommand>
{
    public DispatchShipmentCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
        RuleFor(x => x.Carrier).NotEmpty();
        RuleFor(x => x.TrackingCode).NotEmpty();
    }
}
