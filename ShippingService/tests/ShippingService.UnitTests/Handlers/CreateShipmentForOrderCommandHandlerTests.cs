using ShippingService.Application.Commands;
using ShippingService.Application.Handlers;
using ShippingService.Application.Interfaces;
using ShippingService.Domain.Entities;
using NSubstitute;

namespace ShippingService.UnitTests.Handlers;

public class CreateShipmentForOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_NoExistingShipment_CreatesAwaitingFulfillmentShipment()
    {
        var shipmentRepository = Substitute.For<IShipmentRepository>();
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        shipmentRepository.GetByOrderIdAsync(orderId, Arg.Any<CancellationToken>()).Returns((Shipment?)null);

        var handler = new CreateShipmentForOrderCommandHandler(shipmentRepository);
        var result = await handler.Handle(new CreateShipmentForOrderCommand(orderId, userId, "1 Main St"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("AwaitingFulfillment", result.Value!.Status);
        Assert.Equal("1 Main St", result.Value.ShippingAddress);
        await shipmentRepository.Received(1).AddAsync(
            Arg.Is<Shipment>(s => s != null && s.OrderId == orderId && s.UserId == userId && s.Status == ShipmentStatus.AwaitingFulfillment),
            Arg.Any<CancellationToken>());
        await shipmentRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShipmentAlreadyExistsForOrder_ReturnsExistingWithoutCreatingDuplicate()
    {
        var shipmentRepository = Substitute.For<IShipmentRepository>();
        var orderId = Guid.NewGuid();
        var existing = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            UserId = Guid.NewGuid(),
            ShippingAddress = "Existing Address",
            Status = ShipmentStatus.Dispatched,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        shipmentRepository.GetByOrderIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(existing);

        var handler = new CreateShipmentForOrderCommandHandler(shipmentRepository);
        var result = await handler.Handle(new CreateShipmentForOrderCommand(orderId, existing.UserId, "New Address"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(existing.Id, result.Value!.Id);
        Assert.Equal("Existing Address", result.Value.ShippingAddress);
        await shipmentRepository.DidNotReceive().AddAsync(Arg.Any<Shipment>(), Arg.Any<CancellationToken>());
    }
}
