using InventoryService.Application.Commands;
using FluentValidation;

namespace InventoryService.Application.Validators;

public class ProvisionInventoryForProductCommandValidator : AbstractValidator<ProvisionInventoryForProductCommand>
{
    public ProvisionInventoryForProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
