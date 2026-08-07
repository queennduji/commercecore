using InventoryService.Application.Common;
using MediatR;

namespace InventoryService.Application.Commands;

public record DeactivateLocationCommand(Guid Id) : IRequest<ServiceResult<bool>>;
