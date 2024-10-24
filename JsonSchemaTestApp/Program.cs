using Json.More;
using Json.Path;
using Json.Schema;
using JsonSchemaTestApp.JsonSchemaBuilder;
using JsonSchemaTestApp.JsonSchemaDataProvider;
using JsonSchemaTestApp.JsonSchemaValidator;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

IServiceCollection services = new ServiceCollection();

services.AddScoped<IJsonSchemaBuilder, CustomJsonSchemaBuilder>();
services.AddScoped<IJsonSchemaDataProvider, GraphQLJsonSchemaDataProvider>();

services.AddGraphQL().AddQueryType<MyQueries>();

var serviceProvider = services.BuildServiceProvider();

CancellationToken cancellationToken = CancellationToken.None;

var jsonSchemaBuilder = serviceProvider.GetRequiredService<IJsonSchemaBuilder>();

RegisterGlobalSchemas();

// Read Schema
var schemaFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "schemas/mexico/absence/template.json");
var schemaText = File.ReadAllText(schemaFilePath);

var schemaBuilder = serviceProvider.GetRequiredService<IJsonSchemaBuilder>();
var builtSchemaNode = await schemaBuilder.BuildAsync(schemaText, null, cancellationToken);

string data =
    """
    {
      "requestData": {
        "user": {
          "id": "00000000-0000-0000-0000-000000000000",
          "displayName": "John Doe"
        },
        "reasonDetails": {
          "name": "Vacation",
          "affectingBalance": true,
          "medicalCertificateRequired": false
        },
        "leavePeriod": {
          "start": "2024-09-06",
          "end": "2024-09-07"
        },
        "type": "Morning",
        "travelDays": true,
        "departurePeriod": {
          "start": "2024-09-06",
          "end": "2024-09-07"
        },
        "arrivalPeriod": {
          "start": "2024-09-06",
          "end": "2024-09-07"
        }
      }
    }
    """;

var jsonData = JsonNode.Parse(data);

var jsonSerializerOptions = new JsonSerializerOptions();
jsonSerializerOptions.Converters.Add(new CustomObjectKeywordJsonConverter());

var jsonSchemaRaw = JsonSchema.FromText(builtSchemaNode.ToJsonString(), jsonSerializerOptions);
jsonSchemaRaw.PatchBaseUri();

var result = jsonSchemaRaw.Evaluate(jsonData, new EvaluationOptions {  ProcessCustomKeywords = true, OutputFormat = OutputFormat.List });
Console.WriteLine($"IsValid: {result.IsValid}");
Console.WriteLine(GetErrors(result));
Console.WriteLine();

var bundle = jsonSchemaRaw.Bundle();
var bundleText = bundle.ToJsonDocument().RootElement.GetRawText();

Console.WriteLine("BUNDLE");
Console.WriteLine(bundleText);
Console.WriteLine();

var formlyBundle = jsonSchemaRaw.GetFormlyBundle();

Console.WriteLine("CUSTOM BUNDLE");
Console.WriteLine(formlyBundle);
Console.WriteLine();

var formlyBundleSchema = JsonSchema.FromText(formlyBundle, jsonSerializerOptions);

var result2 = formlyBundleSchema.Evaluate(jsonData, new EvaluationOptions { ProcessCustomKeywords = true, OutputFormat = OutputFormat.List });
Console.WriteLine($"IsValid: {result.IsValid}");
Console.WriteLine(GetErrors(result2));
Console.WriteLine();

static string GetErrors(EvaluationResults result)
{
    return result.Details
        .Where(d => !d.IsValid)
        .Where(e => e.HasErrors)
        .ToJsonDocument().RootElement.GetRawText();
}

static void RegisterGlobalSchemas()
{
    SchemaKeywordRegistry.Register<CustomObjectKeyword>();

    Vocabulary CustomObjectVocabulary = new Vocabulary("http://localhost/vocabulary", typeof(CustomObjectKeyword));
    VocabularyRegistry.Register(CustomObjectVocabulary);

    var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "schemas/common/");

    var files = Directory.GetFiles(path, "*.json");
    foreach (var file in files)
    {
        var jsonSerializerOptions = new JsonSerializerOptions();
        jsonSerializerOptions.Converters.Add(new CustomObjectKeywordJsonConverter());

        var schema = JsonSchema.FromFile(file, jsonSerializerOptions);
        schema.PatchBaseUri();

        SchemaRegistry.Global.Register(schema);
    }
}


// Ugly but works (formly doesn't support remote schemas like $ref)


public static class JsonSchemaExtensions
{
    public static JsonSchema PatchBaseUri(this JsonSchema schema)
    {
        var schemaId = schema.GetId();
        schema.BaseUri = new Uri($"https://localhost/{schemaId}");
        return schema;
    }

    public static string GetFormlyBundle(this JsonSchema jsonSchema)
    {
        const string IdKeywordName = IdKeyword.Name;
        const string DefsKeywordName = DefsKeyword.Name;
        const string DefinitionsKeywordName = DefinitionsKeyword.Name;
        const string RefKeywordName = RefKeyword.Name;

        var jsonSerializerOptions = new JsonSerializerOptions();
        jsonSerializerOptions.Converters.Add(new CustomObjectKeywordJsonConverter());

        var schemaObject = JsonSerializer.SerializeToNode(jsonSchema.Bundle().ToJsonDocument(), jsonSerializerOptions);

        var schemaId = jsonSchema.GetId()!.OriginalString;

        var rootPath = JsonPath.Parse($"$['{DefsKeywordName}'][?(@['{IdKeywordName}'] == '{schemaId}')]");
        var externalDefinitionsPath = JsonPath.Parse($"$['{DefsKeywordName}'][?(@['{IdKeywordName}'] != '{schemaId}')]");

        JsonNode root = rootPath.Evaluate(schemaObject).Matches.FirstOrDefault()?.Value!;
        IEnumerable<JsonNode?> externalDefinitions = externalDefinitionsPath.Evaluate(schemaObject).Matches.Select(n => n.Value);

        foreach (var externalDefinition in externalDefinitions)
        {
            // Find the internal definitions which have a $ref to an external definition
            var externalDefinitionId = externalDefinition![IdKeywordName]!.AsValue().GetString();
            var internalDefinitionPath = JsonPath.Parse($"$['{DefinitionsKeywordName}'][?(@['{RefKeywordName}'] == '{externalDefinitionId}')]");

            var internalDefinitions = internalDefinitionPath.Evaluate(root).Matches.Select(n => n.Value);

            // Replace the internal definition with a copy of the external definition
            foreach(var internalDefinition in internalDefinitions.ToArray())
            {
                // Clone the external definition and remove the $id
                var definitionCopy = externalDefinition.DeepClone();
                definitionCopy.AsObject().Remove(IdKeywordName);

                internalDefinition!.ReplaceWith(definitionCopy);
            }
        }

        return root.ToString();
    }
}