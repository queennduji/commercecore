using ShippingService.Application.Handlers;
using ShippingService.Application.Interfaces;
using ShippingService.Application.Queries;
using ShippingService.Domain.Entities;
using NSubstitute;

namespace ShippingService.UnitTests.Handlers;

public class GetShipmentByOrderQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShipmentOwnedByCaller_ReturnsIt()
    {
        var shipmentRepository = Substitute.For<IShipmentRepository>();
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var shipment = new Shipment { Id = Guid.NewGuid(), OrderId = orderId, UserId = userId, ShippingAddress = "1 Main St", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        shipmentRepository.GetByOrderIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(shipment);

        var handler = new GetShipmentByOrderQueryHandler(shipmentRepository);
        var result = await handler.Handle(new GetShipmentByOrderQuery(orderId, userId), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(shipment.Id, result.Value!.Id);
    }

    [Fact]
    public async Task Handle_ShipmentOwnedByDifferentUser_ReturnsNotFoundStyleFailure()
    {
        var shipmentRepository = Substitute.For<IShipmentRepository>();
        var orderId = Guid.NewGuid();
        var shipment = new Shipment { Id = Guid.NewGuid(), OrderId = orderId, UserId = Guid.NewGuid(), ShippingAddress = "1 Main St", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        shipmentRepository.GetByOrderIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(shipment);

        var handler = new GetShipmentByOrderQueryHandler(shipmentRepository);
        var result = await handler.Handle(new GetShipmentByOrderQuery(orderId, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Handle_NoShipmentForOrder_ReturnsFailure()
    {
        var shipmentRepository = Substitute.For<IShipmentRepository>();
        shipmentRepository.GetByOrderIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Shipment?)null);

        var handler = new GetShipmentByOrderQueryHandler(shipmentRepository);
        var result = await handler.Handle(new GetShipmentByOrderQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Succeeded);
    }
}
