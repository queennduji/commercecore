using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using MediatR;

namespace InventoryService.Application.Queries;

public record GetInventoryItemQuery(Guid ProductId, Guid LocationId) : IRequest<ServiceResult<InventoryItemDto>>;
