using InventoryService.Application.Handlers;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Queries;
using InventoryService.Domain.Entities;
using NSubstitute;

namespace InventoryService.UnitTests.Handlers;

public class ListLocationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllLocations()
    {
        var locationRepository = Substitute.For<ILocationRepository>();
        locationRepository.ListAsync(Arg.Any<CancellationToken>()).Returns(new List<Location>
        {
            new() { Id = Guid.NewGuid(), Name = "A", Code = "WH-A" },
            new() { Id = Guid.NewGuid(), Name = "B", Code = "WH-B" }
        });

        var handler = new ListLocationsQueryHandler(locationRepository);
        var result = await handler.Handle(new ListLocationsQuery(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Value!.Count);
    }
}
