using Avro;
using Avro.Specific;

namespace NotificationService.Infrastructure.Messaging.Schemas;

/// <summary>Mirrors PaymentService's PaymentFailedAvro exactly.</summary>
public class PaymentFailedAvro : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "PaymentFailedEvent",
          "namespace": "CommerceCore.PaymentService.Events",
          "fields": [
            {"name": "paymentId", "type": "string"},
            {"name": "orderId", "type": "string"},
            {"name": "userId", "type": "string"},
            {"name": "failureReason", "type": "string"},
            {"name": "failedAt", "type": {"type": "long", "logicalType": "timestamp-millis"}}
          ]
        }
        """);

    public string PaymentId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => PaymentId,
        1 => OrderId,
        2 => UserId,
        3 => FailureReason,
        4 => FailedAt,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: PaymentId = (string)fieldValue; break;
            case 1: OrderId = (string)fieldValue; break;
            case 2: UserId = (string)fieldValue; break;
            case 3: FailureReason = (string)fieldValue; break;
            case 4: FailedAt = (DateTime)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
