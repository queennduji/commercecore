using InventoryService.Application.Commands;
using FluentValidation;

namespace InventoryService.Application.Validators;

public class CommitReservationCommandValidator : AbstractValidator<CommitReservationCommand>
{
    public CommitReservationCommandValidator()
    {
        RuleFor(x => x.ReservationId).NotEmpty();
    }
}
