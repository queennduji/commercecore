using Avro;
using Avro.Specific;

namespace CatalogService.Infrastructure.Messaging.Schemas;

public class ProductUpdatedAvro : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "ProductUpdatedEvent",
          "namespace": "CommerceCore.CatalogService.Events",
          "fields": [
            {"name": "productId", "type": "string"},
            {"name": "name", "type": "string"},
            {"name": "price", "type": "double"},
            {"name": "status", "type": "string"},
            {"name": "categoryId", "type": "string"},
            {"name": "updatedAt", "type": {"type": "long", "logicalType": "timestamp-millis"}}
          ]
        }
        """);

    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => ProductId,
        1 => Name,
        2 => Price,
        3 => Status,
        4 => CategoryId,
        5 => UpdatedAt,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: ProductId = (string)fieldValue; break;
            case 1: Name = (string)fieldValue; break;
            case 2: Price = (double)fieldValue; break;
            case 3: Status = (string)fieldValue; break;
            case 4: CategoryId = (string)fieldValue; break;
            case 5: UpdatedAt = (DateTime)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
