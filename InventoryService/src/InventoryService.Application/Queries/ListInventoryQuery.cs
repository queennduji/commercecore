using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using MediatR;

namespace InventoryService.Application.Queries;

public record ListInventoryQuery(
    Guid? ProductId,
    Guid? LocationId,
    int Page = 1,
    int PageSize = 20) : IRequest<ServiceResult<PagedResult<InventoryItemDto>>>;
