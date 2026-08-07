using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using MediatR;

namespace InventoryService.Application.Commands;

public record ReserveStockCommand(
    Guid ProductId,
    Guid LocationId,
    int Quantity,
    string? ReferenceId) : IRequest<ServiceResult<StockReservationDto>>;
