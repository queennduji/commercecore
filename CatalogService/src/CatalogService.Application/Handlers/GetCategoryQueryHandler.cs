using CatalogService.Application.Common;
using CatalogService.Application.Dtos;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Mapping;
using CatalogService.Application.Queries;
using MediatR;

namespace CatalogService.Application.Handlers;

public class GetCategoryQueryHandler : IRequestHandler<GetCategoryQuery, ServiceResult<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoryQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<ServiceResult<CategoryDto>> Handle(GetCategoryQuery request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);
        return category is null
            ? ServiceResult<CategoryDto>.Failure("Category not found.")
            : ServiceResult<CategoryDto>.Success(category.ToDto());
    }
}
