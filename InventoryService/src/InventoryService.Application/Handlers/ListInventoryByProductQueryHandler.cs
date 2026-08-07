using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Mapping;
using InventoryService.Application.Queries;
using MediatR;

namespace InventoryService.Application.Handlers;

public class ListInventoryByProductQueryHandler : IRequestHandler<ListInventoryByProductQuery, ServiceResult<IReadOnlyList<InventoryItemDto>>>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;

    public ListInventoryByProductQueryHandler(IInventoryItemRepository inventoryItemRepository)
    {
        _inventoryItemRepository = inventoryItemRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<InventoryItemDto>>> Handle(ListInventoryByProductQuery request, CancellationToken cancellationToken)
    {
        var items = await _inventoryItemRepository.ListByProductIdAsync(request.ProductId, cancellationToken);
        return ServiceResult<IReadOnlyList<InventoryItemDto>>.Success(items.Select(i => i.ToDto()).ToList());
    }
}
