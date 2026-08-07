using InventoryService.Application.Commands;
using FluentValidation;

namespace InventoryService.Application.Validators;

public class ReleaseReservationCommandValidator : AbstractValidator<ReleaseReservationCommand>
{
    public ReleaseReservationCommandValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty();
    }
}
