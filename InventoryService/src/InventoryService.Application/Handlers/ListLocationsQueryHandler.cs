using InventoryService.Application.Common;
using InventoryService.Application.Dtos;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Mapping;
using InventoryService.Application.Queries;
using MediatR;

namespace InventoryService.Application.Handlers;

public class ListLocationsQueryHandler : IRequestHandler<ListLocationsQuery, ServiceResult<IReadOnlyList<LocationDto>>>
{
    private readonly ILocationRepository _locationRepository;

    public ListLocationsQueryHandler(ILocationRepository locationRepository)
    {
        _locationRepository = locationRepository;
    }

    public async Task<ServiceResult<IReadOnlyList<LocationDto>>> Handle(ListLocationsQuery request, CancellationToken cancellationToken)
    {
        var locations = await _locationRepository.ListAsync(cancellationToken);
        return ServiceResult<IReadOnlyList<LocationDto>>.Success(locations.Select(l => l.ToDto()).ToList());
    }
}
