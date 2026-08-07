using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using MediatR;

namespace InventoryService.Application.Commands;

public record UpdateLocationCommand(
    Guid Id,
    string Name,
    string Code,
    bool IsActive) : IRequest<ServiceResult<LocationDto>>;
