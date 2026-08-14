using Avro;
using Avro.Specific;

namespace ShippingService.Infrastructure.Messaging.Schemas;

public class ShipmentExceptionAvro : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "ShipmentExceptionEvent",
          "namespace": "CommerceCore.ShippingService.Events",
          "fields": [
            {"name": "shipmentId", "type": "string"},
            {"name": "orderId", "type": "string"},
            {"name": "userId", "type": "string"},
            {"name": "reason", "type": "string"},
            {"name": "occurredAt", "type": {"type": "long", "logicalType": "timestamp-millis"}}
          ]
        }
        """);

    public string ShipmentId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => ShipmentId,
        1 => OrderId,
        2 => UserId,
        3 => Reason,
        4 => OccurredAt,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: ShipmentId = (string)fieldValue; break;
            case 1: OrderId = (string)fieldValue; break;
            case 2: UserId = (string)fieldValue; break;
            case 3: Reason = (string)fieldValue; break;
            case 4: OccurredAt = (DateTime)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
