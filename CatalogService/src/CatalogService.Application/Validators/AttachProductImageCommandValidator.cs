using CatalogService.Application.Commands;
using FluentValidation;

namespace CatalogService.Application.Validators;

public class AttachProductImageCommandValidator : AbstractValidator<AttachProductImageCommand>
{
    public AttachProductImageCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ObjectKey).NotEmpty();
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
