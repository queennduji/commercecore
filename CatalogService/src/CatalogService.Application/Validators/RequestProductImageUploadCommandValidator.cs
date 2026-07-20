using CatalogService.Application.Commands;
using FluentValidation;

namespace CatalogService.Application.Validators;

public class RequestProductImageUploadCommandValidator : AbstractValidator<RequestProductImageUploadCommand>
{
    public RequestProductImageUploadCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.ContentType).NotEmpty().Must(ct => ct.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            .WithMessage("'Content Type' must be an image content type (e.g. image/png, image/jpeg).");
    }
}
