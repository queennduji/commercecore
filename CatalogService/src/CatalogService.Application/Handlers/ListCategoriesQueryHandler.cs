using CatalogService.Application.Common;
using CatalogService.Application.Dtos;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Mapping;
using CatalogService.Application.Queries;
using MediatR;

namespace CatalogService.Application.Handlers;

public class ListCategoriesQueryHandler : IRequestHandler<ListCategoriesQuery, ServiceResult<IReadOnlyList<CategoryDto>>>
{
    private readonly ICategoryRepository _categoryRepository;

    public ListCategoriesQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<CategoryDto>>> Handle(ListCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.ListAsync(cancellationToken);
        IReadOnlyList<CategoryDto> dtos = categories.Select(c => c.ToDto()).ToList();
        return ServiceResult<IReadOnlyList<CategoryDto>>.Success(dtos);
    }
}
