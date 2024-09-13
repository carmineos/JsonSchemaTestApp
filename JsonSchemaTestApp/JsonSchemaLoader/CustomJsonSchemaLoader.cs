using NJsonSchema;
using NJsonSchema.Generation;
using System.Collections.Concurrent;
using System.Reflection;

namespace JsonSchemaTestApp.JsonSchemaLoader;

public class CustomJsonSchemaLoader : IJsonSchemaLoader
{
    //private readonly IWebHostEnvironment _webHostEnvironment;

    //public CustomJsonSchemaLoader(IWebHostEnvironment webHostEnvironment)
    //{
    //    _webHostEnvironment = webHostEnvironment;
    //}

    private ConcurrentDictionary<string, JsonSchema> _commonSchemas;

    public async Task<JsonSchema> FromJsonAsync(string schema, CancellationToken cancellationToken)
    {
        if (_commonSchemas is null)
        {
            _commonSchemas = await ReadFilesAsync(cancellationToken);
        }

        JsonSchema jsonSchema = await JsonSchema.FromJsonAsync(schema, null, rootSchema =>
        {
            JsonSchemaResolver schemaResolver = new JsonSchemaResolver(rootSchema, new SystemTextJsonSchemaGeneratorSettings());

            var resolver = new JsonReferenceResolver(schemaResolver);

            foreach (var commonSchema in _commonSchemas)
            {
                var id = commonSchema.Value.Id ?? commonSchema.Value.ExtensionData["$id"]?.ToString();

                resolver.AddDocumentReference(id, commonSchema.Value);

                rootSchema.Definitions[commonSchema.Key] = commonSchema.Value;
            }

            return resolver;
        }, cancellationToken);

        return jsonSchema;
    }

    // TODO: Cache files
    private async Task<ConcurrentDictionary<string, JsonSchema>> ReadFilesAsync(CancellationToken cancellationToken)
    {
        //var files = _webHostEnvironment.WebRootFileProvider
        //    .GetDirectoryContents("Schemas/Common/")
        //    .Where(f => Path.GetExtension(f.Name) == ".json");

        var files = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Schemas/Common/"))
            .GetFiles("*.json");

        ConcurrentDictionary<string, JsonSchema> schemas = new();

        await Parallel.ForEachAsync(files, cancellationToken, async (item, token) =>
        {
            var schema = await JsonSchema.FromFileAsync(item.FullName, token);

            schemas.TryAdd(Path.GetFileNameWithoutExtension(item.Name), schema);
        });

        return schemas;
    }
}
