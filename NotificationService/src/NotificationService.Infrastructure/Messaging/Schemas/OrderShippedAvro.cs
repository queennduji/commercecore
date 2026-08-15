using Avro;
using Avro.Specific;

namespace NotificationService.Infrastructure.Messaging.Schemas;

/// <summary>Mirrors OrderService's OrderShippedAvro exactly.</summary>
public class OrderShippedAvro : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "OrderShippedEvent",
          "namespace": "CommerceCore.OrderService.Events",
          "fields": [
            {"name": "orderId", "type": "string"},
            {"name": "userId", "type": "string"},
            {"name": "shippedAt", "type": {"type": "long", "logicalType": "timestamp-millis"}}
          ]
        }
        """);

    public string OrderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime ShippedAt { get; set; }

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => OrderId,
        1 => UserId,
        2 => ShippedAt,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: OrderId = (string)fieldValue; break;
            case 1: UserId = (string)fieldValue; break;
            case 2: ShippedAt = (DateTime)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
