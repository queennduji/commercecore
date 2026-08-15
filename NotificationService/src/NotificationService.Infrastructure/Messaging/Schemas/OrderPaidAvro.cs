using Avro;
using Avro.Specific;

namespace NotificationService.Infrastructure.Messaging.Schemas;

/// <summary>Mirrors OrderService's OrderPaidAvro exactly (including the shippingAddress field
/// added when ShippingService started consuming this topic — unused here, but the schema must
/// still match field-for-field).</summary>
public class OrderPaidAvro : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "OrderPaidEvent",
          "namespace": "CommerceCore.OrderService.Events",
          "fields": [
            {"name": "orderId", "type": "string"},
            {"name": "userId", "type": "string"},
            {"name": "paidAt", "type": {"type": "long", "logicalType": "timestamp-millis"}},
            {"name": "shippingAddress", "type": "string", "default": ""}
          ]
        }
        """);

    public string OrderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime PaidAt { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => OrderId,
        1 => UserId,
        2 => PaidAt,
        3 => ShippingAddress,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: OrderId = (string)fieldValue; break;
            case 1: UserId = (string)fieldValue; break;
            case 2: PaidAt = (DateTime)fieldValue; break;
            case 3: ShippingAddress = (string)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
