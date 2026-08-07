using Avro;
using Avro.Specific;

namespace InventoryService.Infrastructure.Messaging.Schemas;

public class StockAdjustedAvro : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "StockAdjustedEvent",
          "namespace": "CommerceCore.InventoryService.Events",
          "fields": [
            {"name": "productId", "type": "string"},
            {"name": "locationId", "type": "string"},
            {"name": "delta", "type": "int"},
            {"name": "onHandAfter", "type": "int"},
            {"name": "reason", "type": "string"},
            {"name": "adjustedAt", "type": {"type": "long", "logicalType": "timestamp-millis"}}
          ]
        }
        """);

    public string ProductId { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public int Delta { get; set; }
    public int OnHandAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime AdjustedAt { get; set; }

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => ProductId,
        1 => LocationId,
        2 => Delta,
        3 => OnHandAfter,
        4 => Reason,
        5 => AdjustedAt,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: ProductId = (string)fieldValue; break;
            case 1: LocationId = (string)fieldValue; break;
            case 2: Delta = (int)fieldValue; break;
            case 3: OnHandAfter = (int)fieldValue; break;
            case 4: Reason = (string)fieldValue; break;
            case 5: AdjustedAt = (DateTime)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
