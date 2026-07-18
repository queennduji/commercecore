using Avro;
using Avro.Specific;

namespace AuthenticationService.Infrastructure.Messaging.Schemas;

public class UserRegisteredAvro : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "UserRegisteredEvent",
          "namespace": "CommerceCore.AuthenticationService.Events",
          "fields": [
            {"name": "userId", "type": "string"},
            {"name": "email", "type": "string"},
            {"name": "registeredAt", "type": {"type": "long", "logicalType": "timestamp-millis"}}
          ]
        }
        """);

    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => UserId,
        1 => Email,
        2 => RegisteredAt,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: UserId = (string)fieldValue; break;
            case 1: Email = (string)fieldValue; break;
            case 2: RegisteredAt = (DateTime)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
