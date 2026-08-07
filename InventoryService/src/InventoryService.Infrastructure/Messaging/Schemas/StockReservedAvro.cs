using Avro;
using Avro.Specific;

namespace InventoryService.Infrastructure.Messaging.Schemas;

public class StockReservedAvro : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "StockReservedEvent",
          "namespace": "CommerceCore.InventoryService.Events",
          "fields": [
            {"name": "reservationId", "type": "string"},
            {"name": "productId", "type": "string"},
            {"name": "locationId", "type": "string"},
            {"name": "quantity", "type": "int"},
            {"name": "referenceId", "type": ["null", "string"], "default": null},
            {"name": "reservedAt", "type": {"type": "long", "logicalType": "timestamp-millis"}}
          ]
        }
        """);

    public string ReservationId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? ReferenceId { get; set; }
    public DateTime ReservedAt { get; set; }

    public Schema Schema => _SCHEMA;

    public object? Get(int fieldPos) => fieldPos switch
    {
        0 => ReservationId,
        1 => ProductId,
        2 => LocationId,
        3 => Quantity,
        4 => ReferenceId,
        5 => ReservedAt,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: ReservationId = (string)fieldValue; break;
            case 1: ProductId = (string)fieldValue; break;
            case 2: LocationId = (string)fieldValue; break;
            case 3: Quantity = (int)fieldValue; break;
            case 4: ReferenceId = (string?)fieldValue; break;
            case 5: ReservedAt = (DateTime)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
