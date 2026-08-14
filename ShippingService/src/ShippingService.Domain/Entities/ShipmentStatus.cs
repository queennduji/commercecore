namespace ShippingService.Domain.Entities;

/// <summary>Mirrors the meaningful subset of EasyPost's Tracker.Status values, collapsed into the
/// states this service actually acts on:
/// pre_transit -> Dispatched; in_transit/out_for_delivery/available_for_pickup -> InTransit;
/// delivered -> Delivered; return_to_sender/failure/cancelled/error -> Exception.</summary>
public enum ShipmentStatus
{
    AwaitingFulfillment,
    Dispatched,
    InTransit,
    Delivered,
    Exception
}
