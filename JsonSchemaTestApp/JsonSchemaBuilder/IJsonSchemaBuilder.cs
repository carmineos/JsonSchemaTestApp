using NJsonSchema;

namespace JsonSchemaTestApp.JsonSchemaBuilder;

public interface IJsonSchemaBuilder
{
    Task<JsonSchema> BuildAsync(string inputJsonString, Dictionary<string, object> additionalVariables, CancellationToken cancellationToken);
}
