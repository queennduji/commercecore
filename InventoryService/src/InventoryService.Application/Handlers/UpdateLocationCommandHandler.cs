using InventoryService.Application.Commands;
using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Mapping;
using MediatR;

namespace InventoryService.Application.Handlers;

public class UpdateLocationCommandHandler : IRequestHandler<UpdateLocationCommand, ServiceResult<LocationDto>>
{
    private readonly ILocationRepository _locationRepository;

    public UpdateLocationCommandHandler(ILocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    public async Task<ServiceResult<LocationDto>> Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
    {
        var location = await _locationRepository.GetByIdAsync(request.Id, cancellationToken);
        if (location is null)
        {
            return ServiceResult<LocationDto>.Failure("Location not found.");
        }

        if (!string.Equals(location.Code, request.Code, StringComparison.Ordinal))
        {
            var codeOwner = await _locationRepository.GetByCodeAsync(request.Code, cancellationToken);
            if (codeOwner is not null && codeOwner.Id != request.Id)
            {
                return ServiceResult<LocationDto>.Failure("A location with this code already exists.");
            }
        }

        location.Name = request.Name;
        location.Code = request.Code;
        location.IsActive = request.IsActive;
        location.UpdatedAt = DateTime.UtcNow;

        await _locationRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<LocationDto>.Success(location.ToDto());
    }
}
