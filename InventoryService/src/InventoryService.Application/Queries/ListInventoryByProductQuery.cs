using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using MediatR;

namespace InventoryService.Application.Queries;

/// <summary>Stock for one product across every location it has a record at.</summary>
public record ListInventoryByProductQuery(Guid ProductId) : IRequest<ServiceResult<IReadOnlyList<InventoryItemDto>>>;
