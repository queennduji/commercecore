using CatalogService.Application.Common;
using CatalogService.Application.Dtos;
using MediatR;

namespace CatalogService.Application.Commands;

public record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId) : IRequest<ServiceResult<CategoryDto>>;
