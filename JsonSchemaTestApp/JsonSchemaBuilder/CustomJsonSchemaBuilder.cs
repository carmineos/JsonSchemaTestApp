using Json.More;
using Json.Patch;
using Json.Pointer;
using JsonSchemaTestApp.JsonSchemaDataProvider;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonSchemaTestApp.JsonSchemaBuilder;

public class CustomJsonSchemaBuilder : IJsonSchemaBuilder
{
    private readonly IJsonSchemaDataProvider _jsonSchemaDataProvider;

    private const string SCHEMA_KEY = "schema";
    private const string SCHEMA_DEFINITIONS_KEY = "definitions";
    private const string SCHEMA_ENUM_KEY = "enum";
    private const string SCHEMA_TYPE_KEY = "type";
    private const string SCHEMA_TYPE_OBJECT_KEY = "object";
    private const string SCHEMA_TYPE_STRING_KEY = "string";


    private const string GRAPHQL_KEY = "graphQL";
    private const string GRAPHQL_QUERY_KEY = "query";
    private const string GRAPHQL_VARIABLES_KEY = "variables";
    private const string GRAPHQL_DATA_KEY = "data";

    public CustomJsonSchemaBuilder(IJsonSchemaDataProvider jsonSchemaDataProvider)
    {
        _jsonSchemaDataProvider = jsonSchemaDataProvider;
    }

    public async Task<JsonObject> BuildAsync(string inputJsonString, Dictionary<string, object?>? additionalVariables, CancellationToken cancellationToken)
    {
        JsonObject rootObject = JsonNode.Parse(inputJsonString)!.AsObject();

        JsonObject schemaObject = rootObject[SCHEMA_KEY]!.AsObject();
        JsonObject graphQLObject = rootObject[GRAPHQL_KEY]!.AsObject();

        var dataObject = await GetQueryData(graphQLObject, additionalVariables, cancellationToken);

        var result = BuildDefinitionsAsEnum(schemaObject, dataObject);

        return result.AsObject();
    }

    private async Task<string> GetQueryData(JsonObject graphQLObject, Dictionary<string, object?>? additionalVariables, CancellationToken cancellationToken)
    {
        string query = graphQLObject[GRAPHQL_QUERY_KEY]!.AsValue().GetValue<string>();

        JsonNode? variablesNode = graphQLObject[GRAPHQL_VARIABLES_KEY];

        Dictionary<string, object?>? variables = null!;
        
        if (variablesNode is JsonObject variablesObject)
        {
            variables = JsonSerializer.Deserialize<Dictionary<string, object?>>(variablesObject);
        }

        variables ??= [];

        if (additionalVariables is { Count: > 0 })
        {
            foreach (var variable in additionalVariables)
            {
                variables.TryAdd(variable.Key, variable.Value);
            }
        }

        var dataJsonString = await _jsonSchemaDataProvider.QueryDataAsync(query, variables, cancellationToken);

        return dataJsonString;
    }


    private static JsonNode BuildDefinitionsAsEnum(JsonObject schemaObject, string data)
    {
        var dataObject = JsonNode.Parse(data)!.AsObject()[GRAPHQL_DATA_KEY]!.AsObject();

        List<PatchOperation> operations = [];

        foreach (var (propertyName, propertyNode) in dataObject)
        {
            var enumNode = propertyNode;

            var typePointer = JsonPointer.Create(SCHEMA_DEFINITIONS_KEY, propertyName, SCHEMA_TYPE_KEY);

            if (typePointer.TryEvaluate(schemaObject, out JsonNode? typeNode))
            {
                var typeValue = typeNode!.AsValue().GetString();

                if (typeValue == SCHEMA_TYPE_STRING_KEY)
                {
                    enumNode = enumNode!
                        .AsArray()
                        .Select(p => p.AsObject().First().Value)
                        .ToJsonArray();
                }
            }

            var enumPointer = JsonPointer.Create(SCHEMA_DEFINITIONS_KEY, propertyName, SCHEMA_ENUM_KEY);

            if (enumPointer.TryEvaluate(schemaObject, out _))
            {
                operations.Add(PatchOperation.Replace(enumPointer, enumNode));
            }
            else
            {
                operations.Add(PatchOperation.Add(enumPointer, enumNode));
            }
        }

        var patch = new JsonPatch(operations);
        var patchResult = patch.Apply(schemaObject);

        return patchResult.Result!;
    }
}

