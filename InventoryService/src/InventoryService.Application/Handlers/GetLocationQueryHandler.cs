using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Mapping;
using InventoryService.Application.Queries;
using MediatR;

namespace InventoryService.Application.Handlers;

public class GetLocationQueryHandler : IRequestHandler<GetLocationQuery, ServiceResult<LocationDto>>
{
    private readonly ILocationRepository _locationRepository;

    public GetLocationQueryHandler(ILocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    public async Task<ServiceResult<LocationDto>> Handle(GetLocationQuery request, CancellationToken cancellationToken)
    {
        var location = await _locationRepository.GetByIdAsync(request.Id, cancellationToken);
        return location is null
            ? ServiceResult<LocationDto>.Failure("Location not found.")
            : ServiceResult<LocationDto>.Success(location.ToDto());
    }
}
