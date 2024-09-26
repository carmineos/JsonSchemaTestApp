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


    private const string GRAPHQL_KEY = "graphQL";
    private const string GRAPHQL_QUERY_KEY = "query";
    private const string GRAPHQL_VARIABLES_KEY = "variables";
    private const string GRAPHQL_DATA_KEY = "data";

    public CustomJsonSchemaBuilder(IJsonSchemaDataProvider jsonSchemaDataProvider)
    {
        _jsonSchemaDataProvider = jsonSchemaDataProvider;
    }

    public async Task<JsonObject> BuildAsync(string inputJsonString, Dictionary<string, object?> additionalVariables, CancellationToken cancellationToken)
    {
        JsonObject rootObject = JsonNode.Parse(inputJsonString)!.AsObject();

        JsonObject schemaObject = rootObject[SCHEMA_KEY]!.AsObject();
        JsonObject graphQLObject = rootObject[GRAPHQL_KEY]!.AsObject();

        var dataObject = await GetQueryData(graphQLObject, additionalVariables, cancellationToken);

        BuildDefinitionsAsEnum(schemaObject, dataObject);

        return schemaObject;
    }

    private async Task<string> GetQueryData(JsonObject graphQLObject, Dictionary<string, object?> additionalVariables, CancellationToken cancellationToken)
    {
        string query = graphQLObject[GRAPHQL_QUERY_KEY]!.AsValue().GetValue<string>();

        JsonNode? variablesNode = graphQLObject[GRAPHQL_VARIABLES_KEY];

        Dictionary<string, object?> variables = null;
        
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


    private static void BuildDefinitionsAsEnum(JsonObject schemaObject, string data)
    {
        var definitions = schemaObject[SCHEMA_DEFINITIONS_KEY]!.AsObject();

        var dataObject = JsonNode.Parse(data)!.AsObject()[GRAPHQL_DATA_KEY]!.AsObject();

        foreach (var (propertyName, propertyNode) in dataObject)
        {
            var definition = definitions[propertyName];

            if (definition is not JsonObject definitionObject)
                continue;

            if(propertyNode is not JsonArray propertyArray)
                continue;

            definition[SCHEMA_ENUM_KEY] = propertyArray.DeepClone();
        }
    }
}

