using CartService.Application.Commands;
using FluentValidation;

namespace CartService.Application.Validators;

public class MergeCartCommandValidator : AbstractValidator<MergeCartCommand>
{
    public MergeCartCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.SourceCartId).NotEmpty();
        RuleFor(x => x).Must(x => x.UserId != x.SourceCartId)
            .WithMessage("Source cart cannot be the same as the caller's own cart.");
    }
}
