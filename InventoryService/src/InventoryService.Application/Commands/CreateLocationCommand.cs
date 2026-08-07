using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using MediatR;

namespace InventoryService.Application.Commands;

public record CreateLocationCommand(string Name, string Code) : IRequest<ServiceResult<LocationDto>>;
