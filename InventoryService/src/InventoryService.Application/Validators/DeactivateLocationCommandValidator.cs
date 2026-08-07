using InventoryService.Application.Commands;
using FluentValidation;

namespace InventoryService.Application.Validators;

public class DeactivateLocationCommandValidator : AbstractValidator<DeactivateLocationCommand>
{
    public DeactivateLocationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
