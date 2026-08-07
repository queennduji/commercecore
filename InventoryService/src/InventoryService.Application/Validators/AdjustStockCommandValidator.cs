using InventoryService.Application.Commands;
using FluentValidation;

namespace InventoryService.Application.Validators;

public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.LocationId).NotEmpty();
        RuleFor(x => x.Delta).NotEqual(0).WithMessage("Delta must be non-zero.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
