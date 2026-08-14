using ShippingService.Application.Commands;
using FluentValidation;

namespace ShippingService.Application.Validators;

public class RefreshShipmentTrackingCommandValidator : AbstractValidator<RefreshShipmentTrackingCommand>
{
    public RefreshShipmentTrackingCommandValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
    }
}
