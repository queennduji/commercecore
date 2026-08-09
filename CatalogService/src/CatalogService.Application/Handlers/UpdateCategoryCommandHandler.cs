using CatalogService.Application.Commands;
using CatalogService.Application.Common;
using CatalogService.Application.Dtos;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Mapping;
using MediatR;

namespace CatalogService.Application.Handlers;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, ServiceResult<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepository;

    public UpdateCategoryCommandHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<ServiceResult<CategoryDto>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return ServiceResult<CategoryDto>.Failure("Category not found.");
        }

        if (request.ParentCategoryId.HasValue)
        {
            if (request.ParentCategoryId.Value == request.Id)
            {
                return ServiceResult<CategoryDto>.Failure("A category cannot be its own parent.");
            }

            var parent = await _categoryRepository.GetByIdAsync(request.ParentCategoryId.Value, cancellationToken);
            if (parent is null)
            {
                return ServiceResult<CategoryDto>.Failure("Parent category not found.");
            }
        }

        var sibling = await _categoryRepository.GetByNameAndParentAsync(request.Name, request.ParentCategoryId, cancellationToken);
        if (sibling is not null && sibling.Id != request.Id)
        {
            return ServiceResult<CategoryDto>.Failure("A category with this name already exists under this parent.");
        }

        category.Name = request.Name;
        category.Description = request.Description;
        category.ParentCategoryId = request.ParentCategoryId;
        category.UpdatedAt = DateTime.UtcNow;

        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<CategoryDto>.Success(category.ToDto());
    }
}
