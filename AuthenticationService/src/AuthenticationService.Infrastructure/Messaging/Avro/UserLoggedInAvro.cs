using Avro;
using Avro.Specific;

namespace AuthenticationService.Infrastructure.Messaging.Schemas;

public class UserLoggedInAvro : ISpecificRecord
{
    public static readonly Schema _SCHEMA = Schema.Parse(
        """
        {
          "type": "record",
          "name": "UserLoggedInEvent",
          "namespace": "CommerceCore.AuthenticationService.Events",
          "fields": [
            {"name": "userId", "type": "string"},
            {"name": "loggedInAt", "type": {"type": "long", "logicalType": "timestamp-millis"}}
          ]
        }
        """);

    public string UserId { get; set; } = string.Empty;
    public DateTime LoggedInAt { get; set; }

    public Schema Schema => _SCHEMA;

    public object Get(int fieldPos) => fieldPos switch
    {
        0 => UserId,
        1 => LoggedInAt,
        _ => throw new AvroRuntimeException($"Bad index {fieldPos} in Get()")
    };

    public void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: UserId = (string)fieldValue; break;
            case 1: LoggedInAt = (DateTime)fieldValue; break;
            default: throw new AvroRuntimeException($"Bad index {fieldPos} in Put()");
        }
    }
}
