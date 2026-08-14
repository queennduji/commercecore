using ShippingService.Application.Commands;
using ShippingService.Application.Handlers;
using ShippingService.Application.Interfaces;
using ShippingService.Domain.Entities;
using ShippingService.Domain.Events;
using NSubstitute;

namespace ShippingService.UnitTests.Handlers;

public class DispatchShipmentCommandHandlerTests
{
    private static Shipment NewShipment(ShipmentStatus status = ShipmentStatus.AwaitingFulfillment) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        ShippingAddress = "1 Main St",
        Status = status,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_AwaitingFulfillmentShipment_CreatesTrackerAndPublishesDispatchedEvent()
    {
        var shipmentRepository = Substitute.For<IShipmentRepository>();
        var carrierGateway = Substitute.For<IShippingCarrierGateway>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var shipment = NewShipment();
        shipmentRepository.GetByIdAsync(shipment.Id, Arg.Any<CancellationToken>()).Returns(shipment);
        carrierGateway.CreateTrackerAsync("EZ2000000002", "USPS", Arg.Any<CancellationToken>())
            .Returns(new CarrierTrackerResult(true, "trk_123", "in_transit", null));

        var handler = new DispatchShipmentCommandHandler(shipmentRepository, carrierGateway, eventPublisher);
        var result = await handler.Handle(new DispatchShipmentCommand(shipment.Id, "USPS", "EZ2000000002"), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Dispatched", result.Value!.Status);
        Assert.Equal("USPS", result.Value.CarrierName);
        Assert.Equal("EZ2000000002", result.Value.TrackingNumber);
        await shipmentRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await eventPublisher.Received(1).PublishShipmentDispatchedAsync(
            Arg.Is<ShipmentDispatchedEvent>(e => e != null && e.ShipmentId == shipment.Id && e.OrderId == shipment.OrderId && e.TrackingNumber == "EZ2000000002"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShipmentNotFound_ReturnsFailure()
    {
        var shipmentRepository = Substitute.For<IShipmentRepository>();
        var carrierGateway = Substitute.For<IShippingCarrierGateway>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        shipmentRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Shipment?)null);

        var handler = new DispatchShipmentCommandHandler(shipmentRepository, carrierGateway, eventPublisher);
        var result = await handler.Handle(new DispatchShipmentCommand(Guid.NewGuid(), "USPS", "EZ2000000002"), CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Handle_AlreadyDispatchedShipment_ReturnsFailure()
    {
        var shipmentRepository = Substitute.For<IShipmentRepository>();
        var carrierGateway = Substitute.For<IShippingCarrierGateway>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var shipment = NewShipment(ShipmentStatus.Dispatched);
        shipmentRepository.GetByIdAsync(shipment.Id, Arg.Any<CancellationToken>()).Returns(shipment);

        var handler = new DispatchShipmentCommandHandler(shipmentRepository, carrierGateway, eventPublisher);
        var result = await handler.Handle(new DispatchShipmentCommand(shipment.Id, "USPS", "EZ2000000002"), CancellationToken.None);

        Assert.False(result.Succeeded);
        await carrierGateway.DidNotReceive().CreateTrackerAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GatewayFails_ReturnsFailureAndDoesNotTransition()
    {
        var shipmentRepository = Substitute.For<IShipmentRepository>();
        var carrierGateway = Substitute.For<IShippingCarrierGateway>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var shipment = NewShipment();
        shipmentRepository.GetByIdAsync(shipment.Id, Arg.Any<CancellationToken>()).Returns(shipment);
        carrierGateway.CreateTrackerAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CarrierTrackerResult(false, null, null, "Invalid tracking code."));

        var handler = new DispatchShipmentCommandHandler(shipmentRepository, carrierGateway, eventPublisher);
        var result = await handler.Handle(new DispatchShipmentCommand(shipment.Id, "USPS", "bogus"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(ShipmentStatus.AwaitingFulfillment, shipment.Status);
        await eventPublisher.DidNotReceive().PublishShipmentDispatchedAsync(Arg.Any<ShipmentDispatchedEvent>(), Arg.Any<CancellationToken>());
    }
}
