using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using MediatR;

namespace InventoryService.Application.Queries;

public record GetReservationQuery(Guid Id) : IRequest<ServiceResult<StockReservationDto>>;
