using NJsonSchema;

namespace JsonSchemaTestApp.JsonSchemaLoader;

public interface IJsonSchemaLoader
{
    Task<JsonSchema> FromJsonAsync(string schema, CancellationToken cancellationToken);
}