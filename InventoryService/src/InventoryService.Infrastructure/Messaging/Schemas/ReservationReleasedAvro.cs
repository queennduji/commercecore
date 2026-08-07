using Avro;
using Avro.Specific;

namespace InventoryService.Infrastructure.Messaging.Schemas;

public class ReservationReleasedAvro : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "ReservationReleasedEvent",
          "namespace": "CommerceCore.InventoryService.Events",
          "fields": [
            {"name": "reservationId", "type": "string"},
            {"name": "productId", "type": "string"},
            {"name": "locationId", "type": "string"},
            {"name": "quantity", "type": "int"},
            {"name": "releasedAt", "type": {"type": "long", "logicalType": "timestamp-millis"}}
          ]
        }
        """);

    public string ReservationId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ReleasedAt { get; set; }

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => ReservationId,
        1 => ProductId,
        2 => LocationId,
        3 => Quantity,
        4 => ReleasedAt,
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
            case 4: ReleasedAt = (DateTime)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
