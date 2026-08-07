using InventoryService.Application.Handlers;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Queries;
using InventoryService.Domain.Entities;
using NSubstitute;

namespace InventoryService.UnitTests.Handlers;

public class GetLocationQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingLocation_ReturnsDto()
    {
        var locationRepository = Substitute.For<ILocationRepository>();
        var location = new Location { Id = Guid.NewGuid(), Name = "Name", Code = "WH-A", IsActive = true };
        locationRepository.GetByIdAsync(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        var handler = new GetLocationQueryHandler(locationRepository);
        var result = await handler.Handle(new GetLocationQuery(location.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(location.Id, result.Value!.Id);
    }

    [Fact]
    public async Task Handle_UnknownLocation_ReturnsFailure()
    {
        var locationRepository = Substitute.For<ILocationRepository>();
        locationRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Location?)null);

        var handler = new GetLocationQueryHandler(locationRepository);
        var result = await handler.Handle(new GetLocationQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
