using InventoryService.Application.Commands;
using InventoryService.Application.Handlers;
using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using NSubstitute;

namespace InventoryService.UnitTests.Handlers;

public class UpdateLocationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingLocation_UpdatesFields()
    {
        var locationRepository = Substitute.For<ILocationRepository>();
        var location = new Location
        {
            Id = Guid.NewGuid(),
            Name = "Old Name",
            Code = "WH-OLD",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        locationRepository.GetByIdAsync(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        locationRepository.GetByCodeAsync("WH-NEW", Arg.Any<CancellationToken>()).Returns((Location?)null);

        var handler = new UpdateLocationCommandHandler(locationRepository);
        var command = new UpdateLocationCommand(location.Id, "New Name", "WH-NEW", false);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("New Name", result.Value!.Name);
        Assert.Equal("WH-NEW", result.Value.Code);
        Assert.False(result.Value.IsActive);
        await locationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CodeUnchanged_DoesNotCheckUniqueness()
    {
        var locationRepository = Substitute.For<ILocationRepository>();
        var location = new Location { Id = Guid.NewGuid(), Name = "Name", Code = "WH-SAME", IsActive = true };
        locationRepository.GetByIdAsync(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        var handler = new UpdateLocationCommandHandler(locationRepository);
        var command = new UpdateLocationCommand(location.Id, "Name", "WH-SAME", true);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Succeeded);
        await locationRepository.DidNotReceive().GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CodeChangedToAnotherLocationsCode_ReturnsFailure()
    {
        var locationRepository = Substitute.For<ILocationRepository>();
        var location = new Location { Id = Guid.NewGuid(), Name = "Name", Code = "WH-A", IsActive = true };
        var otherLocation = new Location { Id = Guid.NewGuid(), Name = "Other", Code = "WH-B", IsActive = true };
        locationRepository.GetByIdAsync(location.Id, Arg.Any<CancellationToken>()).Returns(location);
        locationRepository.GetByCodeAsync("WH-B", Arg.Any<CancellationToken>()).Returns(otherLocation);

        var handler = new UpdateLocationCommandHandler(locationRepository);
        var command = new UpdateLocationCommand(location.Id, "Name", "WH-B", true);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Handle_UnknownLocation_ReturnsFailure()
    {
        var locationRepository = Substitute.For<ILocationRepository>();
        locationRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Location?)null);

        var handler = new UpdateLocationCommandHandler(locationRepository);
        var result = await handler.Handle(new UpdateLocationCommand(Guid.NewGuid(), "Name", "CODE", true), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
