using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using MediatR;

namespace InventoryService.Application.Commands;

/// <summary>Adjusts on-hand stock at a location. Delta may be positive (restock) or negative (correction/damage).</summary>
public record AdjustStockCommand(
    Guid ProductId,
    Guid LocationId,
    int Delta,
    string Reason) : IRequest<ServiceResult<InventoryItemDto>>;
