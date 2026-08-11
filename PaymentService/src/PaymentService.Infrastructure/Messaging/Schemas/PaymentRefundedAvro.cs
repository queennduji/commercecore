using Avro;
using Avro.Specific;

namespace PaymentService.Infrastructure.Messaging.Schemas;

public class PaymentRefundedAvro : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "PaymentRefundedEvent",
          "namespace": "CommerceCore.PaymentService.Events",
          "fields": [
            {"name": "paymentId", "type": "string"},
            {"name": "orderId", "type": "string"},
            {"name": "userId", "type": "string"},
            {"name": "amount", "type": "double"},
            {"name": "refundedAt", "type": {"type": "long", "logicalType": "timestamp-millis"}}
          ]
        }
        """);

    public string PaymentId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public double Amount { get; set; }
    public DateTime RefundedAt { get; set; }

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => PaymentId,
        1 => OrderId,
        2 => UserId,
        3 => Amount,
        4 => RefundedAt,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: PaymentId = (string)fieldValue; break;
            case 1: OrderId = (string)fieldValue; break;
            case 2: UserId = (string)fieldValue; break;
            case 3: Amount = (double)fieldValue; break;
            case 4: RefundedAt = (DateTime)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
