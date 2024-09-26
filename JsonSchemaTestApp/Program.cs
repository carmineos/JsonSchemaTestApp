using Json.More;
using Json.Schema;
using Json.Schema.Serialization;
using JsonSchemaTestApp.JsonSchemaBuilder;
using JsonSchemaTestApp.JsonSchemaDataProvider;
using JsonSchemaTestApp.JsonSchemaLoader;
using JsonSchemaTestApp.JsonSchemaValidator;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

IServiceCollection services = new ServiceCollection();

services.AddScoped<IJsonSchemaLoader, CustomJsonSchemaLoader>();
services.AddScoped<IJsonSchemaValidator, CustomJsonSchemaValidator>();
services.AddScoped<IJsonSchemaBuilder, CustomJsonSchemaBuilder>();
services.AddScoped<IJsonSchemaDataProvider, MockJsonSchemaDataProvider>();

var serviceProvider = services.BuildServiceProvider();

CancellationToken cancellationToken = CancellationToken.None;

var jsonSchemaLoader = serviceProvider.GetRequiredService<IJsonSchemaLoader>();
var jsonSchemaValidator = serviceProvider.GetRequiredService<IJsonSchemaValidator>();
var jsonSchemaBuilder = serviceProvider.GetRequiredService<IJsonSchemaBuilder>();

RegisterGlobalSchemas();



string data =
    """
    {
      "requestData": {
        "reasonDetails": {
          "name": "Vacation",
          "affectingBalance": true,
          "medicalCertificateRequired": false
        },
        "leavePeriod": {
          "start": "2024-09-06",
          "end": "2024-09-07"
        },
        "type": "Half Day",
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

var filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "schemas/mexico/absence/template.json");

var jsonSerializerOptions = new JsonSerializerOptions();
jsonSerializerOptions.Converters.Add(new CustomObjectKeywordJsonConverter());

var schemaRaw = JsonSchema.FromFile(filePath, jsonSerializerOptions);

var result = schemaRaw.Evaluate(jsonData, new EvaluationOptions { ProcessCustomKeywords = true, OutputFormat = OutputFormat.List });
Console.WriteLine($"IsValid: {result.IsValid}");
Console.WriteLine(GetErrors(result));
Console.WriteLine();

var bundle = schemaRaw.Bundle();
var bundleText = bundle.ToJsonDocument().RootElement.GetRawText();

Console.WriteLine("BUNDLE");
Console.WriteLine(bundleText);
Console.WriteLine();

var formlyBundle = GetFormlyBundle(schemaRaw);

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

    Vocabulary CustomObjectVocabulary = new Vocabulary("http://mydates.com/vocabulary", typeof(CustomObjectKeyword));
    VocabularyRegistry.Register(CustomObjectVocabulary);

    var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "schemas/common/");

    var files = Directory.GetFiles(path, "*.json");
    foreach (var file in files)
    {
        var jsonSerializerOptions = new JsonSerializerOptions();
        jsonSerializerOptions.Converters.Add(new CustomObjectKeywordJsonConverter());

        var schema = JsonSchema.FromFile(file, jsonSerializerOptions);
        SchemaRegistry.Global.Register(schema);
    }
}

// Ugly but works (formly doesn't support remote schemas like $ref)
static string GetFormlyBundle(JsonSchema jsonSchema)
{
    const string IdKeywordName = IdKeyword.Name;
    const string DefsKeywordName = DefsKeyword.Name;
    const string DefinitionsKeywordName = DefinitionsKeyword.Name;
    const string RefKeywordName = RefKeyword.Name;

    var bundle = jsonSchema.Bundle();

    var schemaId  = jsonSchema.GetId()!.OriginalString;

    var jsonSerializerOptions = new JsonSerializerOptions();
    jsonSerializerOptions.Converters.Add(new CustomObjectKeywordJsonConverter());

    var schemaObject = JsonSerializer.SerializeToNode(bundle.ToJsonDocument(), jsonSerializerOptions);

    var schemaDefsObject = schemaObject![DefsKeywordName]!.AsObject();

    JsonObject outputRoot = null!;
    Dictionary<string, JsonNode> outputDefinitions = [];

    // Among all the $defs, the one with $id equals to the input, becames the root
    // all the other $defs become definitions of the root
    foreach (var (propertyName, propertyNode) in schemaDefsObject)
    {
        if (propertyNode![IdKeywordName]!.GetValue<string>() == schemaId)
        {
            outputRoot = propertyNode.AsObject();
        }
        else
        {
            var node = propertyNode.AsObject();
            outputDefinitions.Add(node[IdKeywordName]!.GetValue<string>(), node);
            node.Remove(IdKeywordName);
        }
    }

    var rootDefinitions = outputRoot[DefinitionsKeywordName].AsObject();

    // Iterate all the root definitions and for those with a $ref to a $defs, replace the object with the actual one
    foreach (var (propertyName, propertyNode) in rootDefinitions.DeepClone().AsObject())
    {
        if (propertyNode![RefKeywordName] is not JsonValue refValue)
            continue;

        var refValueString = refValue.GetValue<string>();
        if (outputDefinitions.TryGetValue(refValueString, out JsonNode? node))
        {
            var clonedNode = node!.DeepClone();
            clonedNode.AsObject().Remove(IdKeywordName);
            rootDefinitions[propertyName] = clonedNode;
        }
    }

    return outputRoot.ToJsonString();
}

//static JsonSchema CustomBundle(JsonSchema jsonSchema, EvaluationOptions? options = null)
//{
//    options = EvaluationOptions.From(options ?? EvaluationOptions.Default);

//    options.SchemaRegistry.Register(jsonSchema);

//    var schemasToSearch = new List<JsonSchema>();
//    var searchedSchemas = new List<JsonSchema>(); // uses reference equality
//    var externalSchemas = new Dictionary<string, JsonSchema>();
//    var bundledReferences = new List<Uri>();
//    var referencesToCheck = new List<Uri> { jsonSchema.BaseUri };

//    while (referencesToCheck.Count != 0)
//    {
//        var nextReference = referencesToCheck[0];
//        referencesToCheck.RemoveAt(0);

//        var resolved = options.SchemaRegistry.Get(nextReference);
//        if (resolved is not JsonSchema resolvedSchema)
//            throw new NotSupportedException("Bundling is not supported for non-schema root documents");

//        if (!bundledReferences.Contains(nextReference))
//        {
//            externalSchemas.Add(Guid.NewGuid().ToString("N")[..10], resolvedSchema);
//            bundledReferences.Add(nextReference);
//        }
//        schemasToSearch.Add(resolvedSchema);

//        while (schemasToSearch.Count != 0)
//        {
//            var schema = schemasToSearch[0];
//            schemasToSearch.RemoveAt(0);
//            if (searchedSchemas.Contains(schema)) continue;

//            if (schema.Keywords == null) continue;

//            searchedSchemas.Add(schema);
//            using (var owner = MemoryPool<JsonSchema>.Shared.Rent())
//            {
//                foreach (var subschema in schema.GetSubschemas(owner))
//                {
//                    schemasToSearch.Add(subschema);
//                }
//            }

//            // this handles references that are already bundled.
//            if (schema.BaseUri != nextReference && !bundledReferences.Contains(schema.BaseUri))
//                bundledReferences.Add(schema.BaseUri);

//            var reference = schema.GetRef();
//            if (reference != null)
//            {
//                var newUri = new Uri(schema.BaseUri, reference);
//                if (newUri == schema.BaseUri) continue; // same document

//                referencesToCheck.Add(newUri);
//            }
//        }
//    }

//    return new JsonSchemaBuilder()
//        .Id(jsonSchema.BaseUri.OriginalString + "(bundled)")
//        .Defs(externalSchemas)
//        .Ref(jsonSchema.BaseUri);
//}
