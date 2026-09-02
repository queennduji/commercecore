using Avro;
using Avro.Specific;

namespace OrderService.Infrastructure.Messaging.Schemas;

/// <summary>Mirrors ShippingService's ShipmentDispatchedAvro exactly (same schema, field-for-field)
/// so this service's consumer can deserialize shipment.dispatched.v1 messages – OrderService only
/// consumes this topic, it does not own it or publish to it.</summary>
public class ShipmentDispatchedAvro : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "ShipmentDispatchedEvent",
          "namespace": "CommerceCore.ShippingService.Events",
          "fields": [
            {"name": "shipmentId", "type": "string"},
            {"name": "orderId", "type": "string"},
            {"name": "userId", "type": "string"},
            {"name": "carrierName", "type": "string"},
            {"name": "trackingNumber", "type": "string"},
            {"name": "dispatchedAt", "type": {"type": "long", "logicalType": "timestamp-millis"}}
          ]
        }
        """);

    public string ShipmentId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string CarrierName { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public DateTime DispatchedAt { get; set; }

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => ShipmentId,
        1 => OrderId,
        2 => UserId,
        3 => CarrierName,
        4 => TrackingNumber,
        5 => DispatchedAt,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: ShipmentId = (string)fieldValue; break;
            case 1: OrderId = (string)fieldValue; break;
            case 2: UserId = (string)fieldValue; break;
            case 3: CarrierName = (string)fieldValue; break;
            case 4: TrackingNumber = (string)fieldValue; break;
            case 5: DispatchedAt = (DateTime)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
