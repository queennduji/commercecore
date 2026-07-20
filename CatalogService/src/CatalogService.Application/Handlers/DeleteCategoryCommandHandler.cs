using CatalogService.Application.Commands;
using CatalogService.Application.Common;
using CatalogService.Application.Interfaces;
using MediatR;

namespace CatalogService.Application.Handlers;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, ServiceResult<bool>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;

    public DeleteCategoryCommandHandler(ICategoryRepository categoryRepository, IProductRepository productRepository)
    {
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
    }

    public async Task<ServiceResult<bool>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return ServiceResult<bool>.Failure("Category not found.");
        }

        if (await _productRepository.AnyInCategoryAsync(request.Id, cancellationToken))
        {
            return ServiceResult<bool>.Failure("Cannot delete a category that still has products assigned to it.");
        }

        _categoryRepository.Remove(category);
        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }
}
