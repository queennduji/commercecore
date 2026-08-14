using ShippingService.Application.Handlers;
using ShippingService.Application.Interfaces;
using ShippingService.Application.Queries;
using ShippingService.Domain.Entities;
using NSubstitute;

namespace ShippingService.UnitTests.Handlers;

public class GetShipmentQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShipmentOwnedByCaller_ReturnsIt()
    {
        var shipmentRepository = Substitute.For<IShipmentRepository>();
        var userId = Guid.NewGuid();
        var shipment = new Shipment { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), UserId = userId, ShippingAddress = "1 Main St", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        shipmentRepository.GetByIdAsync(shipment.Id, Arg.Any<CancellationToken>()).Returns(shipment);

        var handler = new GetShipmentQueryHandler(shipmentRepository);
        var result = await handler.Handle(new GetShipmentQuery(shipment.Id, userId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(shipment.Id, result.Value!.Id);
    }

    [Fact]
    public async Task Handle_ShipmentOwnedByDifferentUser_ReturnsNotFoundStyleFailure()
    {
        var shipmentRepository = Substitute.For<IShipmentRepository>();
        var shipment = new Shipment { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), UserId = Guid.NewGuid(), ShippingAddress = "1 Main St", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        shipmentRepository.GetByIdAsync(shipment.Id, Arg.Any<CancellationToken>()).Returns(shipment);

        var handler = new GetShipmentQueryHandler(shipmentRepository);
        var result = await handler.Handle(new GetShipmentQuery(shipment.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("not found", result.Errors.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_ShipmentDoesNotExist_ReturnsFailure()
    {
        var shipmentRepository = Substitute.For<IShipmentRepository>();
        shipmentRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Shipment?)null);

        var handler = new GetShipmentQueryHandler(shipmentRepository);
        var result = await handler.Handle(new GetShipmentQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
