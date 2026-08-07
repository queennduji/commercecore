using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Mapping;
using InventoryService.Application.Queries;
using MediatR;

namespace InventoryService.Application.Handlers;

public class GetInventoryItemQueryHandler : IRequestHandler<GetInventoryItemQuery, ServiceResult<InventoryItemDto>>
{
    private readonly IInventoryItemRepository _inventoryItemRepository;

    public GetInventoryItemQueryHandler(IInventoryItemRepository inventoryItemRepository)
    {
        _inventoryItemRepository = inventoryItemRepository;
    }

    public async Task<ServiceResult<InventoryItemDto>> Handle(GetInventoryItemQuery request, CancellationToken cancellationToken)
    {
        var item = await _inventoryItemRepository.GetByProductAndLocationAsync(request.ProductId, request.LocationId, cancellationToken);
        return item is null
            ? ServiceResult<InventoryItemDto>.Failure("Inventory record not found for this product/location.")
            : ServiceResult<InventoryItemDto>.Success(item.ToDto());
    }
}
