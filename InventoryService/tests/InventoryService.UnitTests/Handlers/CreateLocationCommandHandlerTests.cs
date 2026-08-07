using InventoryService.Application.Commands;
using InventoryService.Application.Handlers;
using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using NSubstitute;

namespace InventoryService.UnitTests.Handlers;

public class CreateLocationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesLocation()
    {
        var locationRepository = Substitute.For<ILocationRepository>();
        locationRepository.GetByCodeAsync("WH-EAST", Arg.Any<CancellationToken>()).Returns((Location?)null);

        var handler = new CreateLocationCommandHandler(locationRepository);
        var result = await handler.Handle(new CreateLocationCommand("East Warehouse", "WH-EAST"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("East Warehouse", result.Value!.Name);
        Assert.Equal("WH-EAST", result.Value.Code);
        Assert.True(result.Value.IsActive);
        await locationRepository.Received(1).AddAsync(Arg.Any<Location>(), Arg.Any<CancellationToken>());
        await locationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateCode_ReturnsFailure()
    {
        var locationRepository = Substitute.For<ILocationRepository>();
        locationRepository.GetByCodeAsync("WH-EAST", Arg.Any<CancellationToken>())
            .Returns(new Location { Id = Guid.NewGuid(), Name = "Existing", Code = "WH-EAST" });

        var handler = new CreateLocationCommandHandler(locationRepository);
        var result = await handler.Handle(new CreateLocationCommand("East Warehouse", "WH-EAST"), CancellationToken.None);

        Assert.False(result.Succeeded);
        await locationRepository.DidNotReceive().AddAsync(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }
}
