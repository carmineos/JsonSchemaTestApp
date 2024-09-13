using JsonSchemaTestApp.JsonSchemaDataProvider;
using JsonSchemaTestApp.JsonSchemaLoader;
using Newtonsoft.Json.Linq;
using NJsonSchema;

namespace JsonSchemaTestApp.JsonSchemaBuilder;

public class CustomJsonSchemaBuilder : IJsonSchemaBuilder
{
    private readonly IJsonSchemaLoader _jsonSchemaLoader;
    private readonly IJsonSchemaDataProvider _jsonSchemaDataProvider;

    private const string SCHEMA_KEY = "schema";
    private const string GRAPHQL_KEY = "graphQL";
    private const string GRAPHQL_QUERY_KEY = "query";
    private const string GRAPHQL_VARIABLES_KEY = "variables";
    private const string GRAPHQL_DATA_KEY = "data";

    public CustomJsonSchemaBuilder(IJsonSchemaDataProvider jsonSchemaDataProvider, IJsonSchemaLoader jsonSchemaLoader)
    {
        _jsonSchemaDataProvider = jsonSchemaDataProvider;
        _jsonSchemaLoader = jsonSchemaLoader;
    }

    public async Task<JsonSchema> BuildAsync(string inputJsonString, Dictionary<string, object?> additionalVariables, CancellationToken cancellationToken)
    {
        JObject inputJson = JObject.Parse(inputJsonString);

        var schema = await _jsonSchemaLoader.FromJsonAsync(inputJson[SCHEMA_KEY]!.ToString(), cancellationToken);

        var query = inputJson[GRAPHQL_KEY][GRAPHQL_QUERY_KEY]!.ToString();
        var variables = inputJson[GRAPHQL_KEY][GRAPHQL_VARIABLES_KEY]!.ToObject<Dictionary<string, object?>>() ?? [];

        if (additionalVariables is { Count: > 0 })
        {
            foreach (var variable in additionalVariables)
            {
                variables.TryAdd(variable.Key, variable.Value);
            }
        }

        var dataJsonString = await _jsonSchemaDataProvider.QueryDataAsync(query, variables, cancellationToken);

        BuildDefinitionsAsEnum(schema, dataJsonString);
        //BuildDefinitionsAsOneOf(schema, dataJsonString);

        return schema;
    }

    private static void BuildDefinitionsAsEnum(JsonSchema schema, string data, string tokenName = "name")
    {
        var dataJson = JObject.Parse(data);

        var dataJsonRoot = dataJson[GRAPHQL_DATA_KEY] as JObject;

        foreach (var property in dataJsonRoot.Properties())
        {
            var definition = schema.Definitions[property.Name];
            definition.Enumeration.Clear();

            JToken propertyValue = property.Value;
            if (propertyValue.Type == JTokenType.Array)
            {
                var enumNames = new JArray();

                foreach (var item in propertyValue.Children())
                {
                    if(item is not JObject itemObject)
                        continue;
                    definition.Enumeration.Add(itemObject); 
                    enumNames.Add(item.SelectToken(tokenName));
                }

                definition.ExtensionData ??= new Dictionary<string, object>();
                definition.ExtensionData["enumNames"] = enumNames;
            }
        }
    }

    private void BuildDefinitionsAsOneOf(JsonSchema schema, string data, string tokenName = "name")
    {
        var dataJson = JObject.Parse(data);

        var dataJsonRoot = dataJson[GRAPHQL_DATA_KEY] as JObject;

        foreach (var property in dataJsonRoot.Properties())
        {
            var definition = schema.Definitions[property.Name];
            definition.OneOf.Clear();

            JToken propertyValue = property.Value;

            if (propertyValue.Type == JTokenType.Array)
            {
                var oneOfArray = new JArray();
                foreach (var item in propertyValue.Children())
                {
                    if (item is not JObject itemObject)
                        continue;

                    var label = item.SelectToken(tokenName);

                    var oneOf = new JObject()
                    {
                        ["title"] = label,
                        ["const"] = itemObject,
                    };

                    oneOfArray.Add(oneOf);
                }

                definition.ExtensionData ??= new Dictionary<string, object?>();
                definition.ExtensionData["oneOf"] = oneOfArray;
            }
        }
    }

    private static void BuildRuntimeData(JsonSchema schema)
    {
        // TODO: Inject runtime data
    }
}

