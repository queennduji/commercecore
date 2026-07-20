using Avro;
using Avro.Specific;

namespace CatalogService.Infrastructure.Messaging.Schemas;

public class ProductDeletedAvro : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "ProductDeletedEvent",
          "namespace": "CommerceCore.CatalogService.Events",
          "fields": [
            {"name": "productId", "type": "string"},
            {"name": "deletedAt", "type": {"type": "long", "logicalType": "timestamp-millis"}}
          ]
        }
        """);

    public string ProductId { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => ProductId,
        1 => DeletedAt,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: ProductId = (string)fieldValue; break;
            case 1: DeletedAt = (DateTime)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
