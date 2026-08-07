using InventoryService.Application.Commands;
using InventoryService.Application.Handlers;
using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using NSubstitute;

namespace InventoryService.UnitTests.Handlers;

public class ProvisionInventoryForProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ActiveLocationsWithNoExistingRecords_CreatesZeroStockItemAtEach()
    {
        var locationRepository = Substitute.For<ILocationRepository>();
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();

        var productId = Guid.NewGuid();
        var locationA = new Location { Id = Guid.NewGuid(), Name = "A", Code = "WH-A", IsActive = true };
        var locationB = new Location { Id = Guid.NewGuid(), Name = "B", Code = "WH-B", IsActive = true };
        locationRepository.ListActiveAsync(Arg.Any<CancellationToken>()).Returns(new List<Location> { locationA, locationB });
        inventoryItemRepository.GetByProductAndLocationAsync(productId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((InventoryItem?)null);

        var handler = new ProvisionInventoryForProductCommandHandler(locationRepository, inventoryItemRepository);
        var result = await handler.Handle(new ProvisionInventoryForProductCommand(productId), CancellationToken.None);

        Assert.True(result.Succeeded);
        await inventoryItemRepository.Received(1).AddAsync(
            Arg.Is<InventoryItem>(i => i.ProductId == productId && i.LocationId == locationA.Id && i.OnHand == 0),
            Arg.Any<CancellationToken>());
        await inventoryItemRepository.Received(1).AddAsync(
            Arg.Is<InventoryItem>(i => i.ProductId == productId && i.LocationId == locationB.Id && i.OnHand == 0),
            Arg.Any<CancellationToken>());
        await inventoryItemRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RecordAlreadyExistsAtALocation_SkipsThatLocation()
    {
        var locationRepository = Substitute.For<ILocationRepository>();
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();

        var productId = Guid.NewGuid();
        var location = new Location { Id = Guid.NewGuid(), Name = "A", Code = "WH-A", IsActive = true };
        locationRepository.ListActiveAsync(Arg.Any<CancellationToken>()).Returns(new List<Location> { location });
        inventoryItemRepository.GetByProductAndLocationAsync(productId, location.Id, Arg.Any<CancellationToken>())
            .Returns(new InventoryItem { Id = Guid.NewGuid(), ProductId = productId, LocationId = location.Id, OnHand = 3 });

        var handler = new ProvisionInventoryForProductCommandHandler(locationRepository, inventoryItemRepository);
        var result = await handler.Handle(new ProvisionInventoryForProductCommand(productId), CancellationToken.None);

        Assert.True(result.Succeeded);
        await inventoryItemRepository.DidNotReceive().AddAsync(Arg.Any<InventoryItem>(), Arg.Any<CancellationToken>());
        await inventoryItemRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoActiveLocations_SucceedsAsNoOp()
    {
        var locationRepository = Substitute.For<ILocationRepository>();
        var inventoryItemRepository = Substitute.For<IInventoryItemRepository>();
        locationRepository.ListActiveAsync(Arg.Any<CancellationToken>()).Returns(new List<Location>());

        var handler = new ProvisionInventoryForProductCommandHandler(locationRepository, inventoryItemRepository);
        var result = await handler.Handle(new ProvisionInventoryForProductCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.Succeeded);
        await inventoryItemRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
