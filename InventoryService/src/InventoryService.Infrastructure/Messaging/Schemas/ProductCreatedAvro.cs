using Avro;
using Avro.Specific;

namespace InventoryService.Infrastructure.Messaging.Schemas;

/// <summary>
/// Mirrors CatalogService's ProductCreatedAvro exactly (same schema, field-for-field) so this
/// service's consumer can deserialize catalog.product-created.v1 messages – InventoryService only
/// consumes this topic, it does not own it or publish to it.
/// </summary>
public class ProductCreatedAvro : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "ProductCreatedEvent",
          "namespace": "CommerceCore.CatalogService.Events",
          "fields": [
            {"name": "productId", "type": "string"},
            {"name": "name", "type": "string"},
            {"name": "sku", "type": "string"},
            {"name": "price", "type": "double"},
            {"name": "categoryId", "type": "string"},
            {"name": "createdAt", "type": {"type": "long", "logicalType": "timestamp-millis"}}
          ]
        }
        """);

    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public double Price { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => ProductId,
        1 => Name,
        2 => Sku,
        3 => Price,
        4 => CategoryId,
        5 => CreatedAt,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: ProductId = (string)fieldValue; break;
            case 1: Name = (string)fieldValue; break;
            case 2: Sku = (string)fieldValue; break;
            case 3: Price = (double)fieldValue; break;
            case 4: CategoryId = (string)fieldValue; break;
            case 5: CreatedAt = (DateTime)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
