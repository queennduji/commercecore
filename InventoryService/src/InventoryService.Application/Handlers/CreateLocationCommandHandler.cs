using InventoryService.Application.Commands;
using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Mapping;
using InventoryService.Domain.Entities;
using MediatR;

namespace InventoryService.Application.Handlers;

public class CreateLocationCommandHandler : IRequestHandler<CreateLocationCommand, ServiceResult<LocationDto>>
{
    private readonly ILocationRepository _locationRepository;

    public CreateLocationCommandHandler(ILocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    public async Task<ServiceResult<LocationDto>> Handle(CreateLocationCommand request, CancellationToken cancellationToken)
    {
        var existing = await _locationRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (existing is not null)
        {
            return ServiceResult<LocationDto>.Failure("A location with this code already exists.");
        }

        var now = DateTime.UtcNow;
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Code = request.Code,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _locationRepository.AddAsync(location, cancellationToken);
        await _locationRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<LocationDto>.Success(location.ToDto());
    }
}
