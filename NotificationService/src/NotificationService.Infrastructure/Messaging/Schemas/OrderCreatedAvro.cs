using Avro;
using Avro.Specific;

namespace NotificationService.Infrastructure.Messaging.Schemas;

/// <summary>Mirrors OrderService's OrderCreatedAvro exactly.</summary>
public class OrderCreatedAvro : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "OrderCreatedEvent",
          "namespace": "CommerceCore.OrderService.Events",
          "fields": [
            {"name": "orderId", "type": "string"},
            {"name": "userId", "type": "string"},
            {"name": "subtotal", "type": "double"},
            {"name": "createdAt", "type": {"type": "long", "logicalType": "timestamp-millis"}}
          ]
        }
        """);

    public string OrderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public double Subtotal { get; set; }
    public DateTime CreatedAt { get; set; }

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => OrderId,
        1 => UserId,
        2 => Subtotal,
        3 => CreatedAt,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: OrderId = (string)fieldValue; break;
            case 1: UserId = (string)fieldValue; break;
            case 2: Subtotal = (double)fieldValue; break;
            case 3: CreatedAt = (DateTime)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
