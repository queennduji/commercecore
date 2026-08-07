using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using MediatR;

namespace InventoryService.Application.Commands;

public record CommitReservationCommand(Guid ReservationId) : IRequest<ServiceResult<StockReservationDto>>;
