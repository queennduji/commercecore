using ShippingService.Application.Commands;
using ShippingService.Application.Handlers;
using ShippingService.Application.Interfaces;
using ShippingService.Domain.Entities;
using ShippingService.Domain.Events;
using NSubstitute;

namespace ShippingService.UnitTests.Handlers;

public class RefreshShipmentTrackingCommandHandlerTests
{
    private static Shipment DispatchedShipment(ShipmentStatus status = ShipmentStatus.Dispatched) => new()
    {
        Id = Guid.NewGuid(),
        OrderId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        ShippingAddress = "1 Main St",
        Status = status,
        CarrierName = "USPS",
        TrackingNumber = "EZ2000000002",
        ProviderTrackerId = "trk_123",
        DispatchedAt = DateTime.UtcNow,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_CarrierReportsDelivered_TransitionsAndPublishesDeliveredEvent()
    {
        var shipmentRepository = Substitute.For<IShipmentRepository>();
        var carrierGateway = Substitute.For<IShippingCarrierGateway>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var shipment = DispatchedShipment(ShipmentStatus.InTransit);
        shipmentRepository.GetByIdAsync(shipment.Id, Arg.Any<CancellationToken>()).Returns(shipment);
        carrierGateway.RetrieveTrackerAsync("trk_123", Arg.Any<CancellationToken>())
            .Returns(new CarrierTrackerResult(true, "trk_123", "delivered", null));

        var handler = new RefreshShipmentTrackingCommandHandler(shipmentRepository, carrierGateway, eventPublisher);
        var result = await handler.Handle(new RefreshShipmentTrackingCommand(shipment.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Delivered", result.Value!.Status);
        Assert.NotNull(result.Value.DeliveredAt);
        await eventPublisher.Received(1).PublishShipmentDeliveredAsync(
            Arg.Is<ShipmentDeliveredEvent>(e => e != null && e.ShipmentId == shipment.Id && e.OrderId == shipment.OrderId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CarrierReportsFailure_TransitionsToExceptionAndPublishesExceptionEvent()
    {
        var shipmentRepository = Substitute.For<IShipmentRepository>();
        var carrierGateway = Substitute.For<IShippingCarrierGateway>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var shipment = DispatchedShipment();
        shipmentRepository.GetByIdAsync(shipment.Id, Arg.Any<CancellationToken>()).Returns(shipment);
        carrierGateway.RetrieveTrackerAsync("trk_123", Arg.Any<CancellationToken>())
            .Returns(new CarrierTrackerResult(true, "trk_123", "failure", null));

        var handler = new RefreshShipmentTrackingCommandHandler(shipmentRepository, carrierGateway, eventPublisher);
        var result = await handler.Handle(new RefreshShipmentTrackingCommand(shipment.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Exception", result.Value!.Status);
        Assert.Equal("failure", result.Value.ExceptionReason);
        await eventPublisher.Received(1).PublishShipmentExceptionAsync(
            Arg.Is<ShipmentExceptionEvent>(e => e != null && e.ShipmentId == shipment.Id && e.Reason == "failure"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CarrierReportsInTransit_UpdatesStatusWithoutPublishingLifecycleEvent()
    {
        var shipmentRepository = Substitute.For<IShipmentRepository>();
        var carrierGateway = Substitute.For<IShippingCarrierGateway>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var shipment = DispatchedShipment();
        shipmentRepository.GetByIdAsync(shipment.Id, Arg.Any<CancellationToken>()).Returns(shipment);
        carrierGateway.RetrieveTrackerAsync("trk_123", Arg.Any<CancellationToken>())
            .Returns(new CarrierTrackerResult(true, "trk_123", "in_transit", null));

        var handler = new RefreshShipmentTrackingCommandHandler(shipmentRepository, carrierGateway, eventPublisher);
        var result = await handler.Handle(new RefreshShipmentTrackingCommand(shipment.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("InTransit", result.Value!.Status);
        await shipmentRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await eventPublisher.DidNotReceive().PublishShipmentDeliveredAsync(Arg.Any<ShipmentDeliveredEvent>(), Arg.Any<CancellationToken>());
        await eventPublisher.DidNotReceive().PublishShipmentExceptionAsync(Arg.Any<ShipmentExceptionEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SameStatusAsCarrierReports_NoOpDoesNotSaveOrPublish()
    {
        var shipmentRepository = Substitute.For<IShipmentRepository>();
        var carrierGateway = Substitute.For<IShippingCarrierGateway>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var shipment = DispatchedShipment(ShipmentStatus.InTransit);
        shipmentRepository.GetByIdAsync(shipment.Id, Arg.Any<CancellationToken>()).Returns(shipment);
        carrierGateway.RetrieveTrackerAsync("trk_123", Arg.Any<CancellationToken>())
            .Returns(new CarrierTrackerResult(true, "trk_123", "in_transit", null));

        var handler = new RefreshShipmentTrackingCommandHandler(shipmentRepository, carrierGateway, eventPublisher);
        var result = await handler.Handle(new RefreshShipmentTrackingCommand(shipment.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        await shipmentRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CarrierReportsUnknownStatus_NoOp()
    {
        var shipmentRepository = Substitute.For<IShipmentRepository>();
        var carrierGateway = Substitute.For<IShippingCarrierGateway>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var shipment = DispatchedShipment();
        shipmentRepository.GetByIdAsync(shipment.Id, Arg.Any<CancellationToken>()).Returns(shipment);
        carrierGateway.RetrieveTrackerAsync("trk_123", Arg.Any<CancellationToken>())
            .Returns(new CarrierTrackerResult(true, "trk_123", "unknown", null));

        var handler = new RefreshShipmentTrackingCommandHandler(shipmentRepository, carrierGateway, eventPublisher);
        var result = await handler.Handle(new RefreshShipmentTrackingCommand(shipment.Id), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(ShipmentStatus.Dispatched, shipment.Status);
        await shipmentRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShipmentNotYetDispatched_ReturnsFailure()
    {
        var shipmentRepository = Substitute.For<IShipmentRepository>();
        var carrierGateway = Substitute.For<IShippingCarrierGateway>();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ShippingAddress = "1 Main St",
            Status = ShipmentStatus.AwaitingFulfillment,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        shipmentRepository.GetByIdAsync(shipment.Id, Arg.Any<CancellationToken>()).Returns(shipment);

        var handler = new RefreshShipmentTrackingCommandHandler(shipmentRepository, carrierGateway, eventPublisher);
        var result = await handler.Handle(new RefreshShipmentTrackingCommand(shipment.Id), CancellationToken.None);

        Assert.False(result.Succeeded);
        await carrierGateway.DidNotReceive().RetrieveTrackerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
