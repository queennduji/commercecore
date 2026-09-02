namespace ShippingService.Domain.Entities;

public class Shipment
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Snapshotted from OrderService's order.paid.v1 event at creation time – display-only,
    /// this service never validates or geocodes it (no real label purchase happens here, see
    /// EasyPostShippingCarrierGateway).</summary>
    public string ShippingAddress { get; set; } = string.Empty;

    public ShipmentStatus Status { get; set; } = ShipmentStatus.AwaitingFulfillment;
    public string? CarrierName { get; set; }
    public string? TrackingNumber { get; set; }

    /// <summary>EasyPost's Tracker id ("trk_...") – what RefreshShipmentTrackingCommand polls by.
    /// Distinct from TrackingNumber, which is the carrier-facing tracking code.</summary>
    public string? ProviderTrackerId { get; set; }

    public string? ExceptionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}
