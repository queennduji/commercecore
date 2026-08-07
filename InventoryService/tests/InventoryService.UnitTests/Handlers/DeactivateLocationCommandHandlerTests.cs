using InventoryService.Application.Commands;
using InventoryService.Application.Handlers;
using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using NSubstitute;

namespace InventoryService.UnitTests.Handlers;

public class DeactivateLocationCommandHandlerTests
{
    [Fact]
    public async Task Handle_LocationWithNoStock_Deactivates()
    {
        var locationRepository = Substitute.For<ILocationRepository>();
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        var location = new Location { Id = Guid.NewGuid(), Name = "Name", Code = "WH-A", IsActive = true };
        locationRepository.GetByIdAsync(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        inventoryItemRepository.AnyStockAtLocationAsync(location.Id, Arg.Any<CancellationToken>()).Returns(false);

        var handler = new DeactivateLocationCommandHandler(locationRepository, inventoryItemRepository);
        var result = await handler.Handle(new DeactivateLocationCommand(location.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.False(location.IsActive);
        await locationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LocationWithStock_ReturnsFailure()
    {
        var locationRepository = Substitute.For<ILocationRepository>();
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        var location = new Location { Id = Guid.NewGuid(), Name = "Name", Code = "WH-A", IsActive = true };
        locationRepository.GetByIdAsync(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        inventoryItemRepository.AnyStockAtLocationAsync(location.Id, Arg.Any<CancellationToken>()).Returns(true);

        var handler = new DeactivateLocationCommandHandler(locationRepository, inventoryItemRepository);
        var result = await handler.Handle(new DeactivateLocationCommand(location.Id), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.True(location.IsActive);
        await locationRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownLocation_ReturnsFailure()
    {
        var locationRepository = Substitute.For<ILocationRepository>();
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        locationRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Location?)null);

        var handler = new DeactivateLocationCommandHandler(locationRepository, inventoryItemRepository);
        var result = await handler.Handle(new DeactivateLocationCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
