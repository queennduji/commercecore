using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using MediatR;

namespace InventoryService.Application.Queries;

public record GetLocationQuery(Guid Id) : IRequest<ServiceResult<LocationDto>>;
