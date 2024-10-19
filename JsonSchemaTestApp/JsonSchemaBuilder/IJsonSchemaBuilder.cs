using System.Text.Json.Nodes;

namespace JsonSchemaTestApp.JsonSchemaBuilder;

public interface IJsonSchemaBuilder
{
    Task<JsonObject> BuildAsync(string inputJsonString, Dictionary<string, object?>? additionalVariables, CancellationToken cancellationToken);
}
