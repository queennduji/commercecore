using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Mapping;
using InventoryService.Application.Queries;
using MediatR;

namespace InventoryService.Application.Handlers;

public class ListInventoryQueryHandler : IRequestHandler<ListInventoryQuery, ServiceResult<PagedResult<InventoryItemDto>>>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;

    public ListInventoryQueryHandler(IInventoryItemRepository inventoryItemRepository)
    {
        _inventoryItemRepository = inventoryItemRepository;
    }

    public async Task<ServiceResult<PagedResult<InventoryItemDto>>> Handle(ListInventoryQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _inventoryItemRepository.ListAsync(
            request.ProductId,
            request.LocationId,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(i => i.ToDto()).ToList();
        var result = new PagedResult<InventoryItemDto>(dtos, request.Page, request.PageSize, totalCount);

        return ServiceResult<PagedResult<InventoryItemDto>>.Success(result);
    }
}
