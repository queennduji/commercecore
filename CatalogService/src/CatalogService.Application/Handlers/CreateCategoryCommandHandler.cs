using CatalogService.Application.Commands;
using CatalogService.Application.Common;
using CatalogService.Application.Dtos;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Mapping;
using CatalogService.Domain.Entities;
using MediatR;

namespace CatalogService.Application.Handlers;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, ServiceResult<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepository;

    public CreateCategoryCommandHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<ServiceResult<CategoryDto>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (request.ParentCategoryId.HasValue)
        {
            var parent = await _categoryRepository.GetByIdAsync(request.ParentCategoryId.Value, cancellationToken);
            if (parent is null)
            {
                return ServiceResult<CategoryDto>.Failure("Parent category not found.");
            }
        }

        var sibling = await _categoryRepository.GetByNameAndParentAsync(request.Name, request.ParentCategoryId, cancellationToken);
        if (sibling is not null)
        {
            return ServiceResult<CategoryDto>.Failure("A category with this name already exists under this parent.");
        }

        var now = DateTime.UtcNow;
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            ParentCategoryId = request.ParentCategoryId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _categoryRepository.AddAsync(category, cancellationToken);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<CategoryDto>.Success(category.ToDto());
    }
}
