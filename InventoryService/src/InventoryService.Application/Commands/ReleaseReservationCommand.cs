using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using MediatR;

namespace InventoryService.Application.Commands;

public record ReleaseReservationCommand(Guid ReservationId) : IRequest<ServiceResult<StockReservationDto>>;
