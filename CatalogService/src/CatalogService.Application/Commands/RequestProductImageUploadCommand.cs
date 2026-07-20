using CatalogService.Application.Common;
using CatalogService.Application.Interfaces;
using MediatR;

namespace CatalogService.Application.Commands;

public record RequestProductImageUploadCommand(
    Guid ProductId,
    string FileName,
    string ContentType) : IRequest<ServiceResult<PresignedUploadUrl>>;
