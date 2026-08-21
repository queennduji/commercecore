using Avro;
using Avro.Specific;

namespace OrderService.Infrastructure.Messaging.Schemas;

/// <summary>Mirrors ShippingService's ShipmentDeliveredAvro exactly (same schema, field-for-field)
/// so this service's consumer can deserialize shipment.delivered.v1 messages — OrderService only
/// consumes this topic, it does not own it or publish to it.</summary>
public class ShipmentDeliveredAvro : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "ShipmentDeliveredEvent",
          "namespace": "CommerceCore.ShippingService.Events",
          "fields": [
            {"name": "shipmentId", "type": "string"},
            {"name": "orderId", "type": "string"},
            {"name": "userId", "type": "string"},
            {"name": "deliveredAt", "type": {"type": "long", "logicalType": "timestamp-millis"}}
          ]
        }
        """);

    public string ShipmentId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime DeliveredAt { get; set; }

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => ShipmentId,
        1 => OrderId,
        2 => UserId,
        3 => DeliveredAt,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: ShipmentId = (string)fieldValue; break;
            case 1: OrderId = (string)fieldValue; break;
            case 2: UserId = (string)fieldValue; break;
            case 3: DeliveredAt = (DateTime)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
