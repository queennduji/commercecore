using CatalogService.Application.Common;
using CatalogService.Application.Dtos;
using MediatR;

namespace CatalogService.Application.Commands;

public record AttachProductImageCommand(
    Guid ProductId,
    string ObjectKey,
    int SortOrder,
    bool IsPrimary) : IRequest<ServiceResult<ProductImageDto>>;
