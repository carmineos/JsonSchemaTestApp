using Json.More;
using Json.Schema;
using JsonSchemaTestApp.JsonSchemaBuilder;
using JsonSchemaTestApp.JsonSchemaDataProvider;
using JsonSchemaTestApp.JsonSchemaLoader;
using JsonSchemaTestApp.JsonSchemaValidator;
using Microsoft.Extensions.DependencyInjection;
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
      "runtimeData": {
        "userData": {
          "profiles": [
            "SysAdmin",
            "HR"
          ],
          "roles": {
            "IsLineManagerOfRequestor": false,
            "IsUnitManagerOfRequestor": false,
            "IsSecondLevelOfRequestor": false,
            "IsDepartmentManager": [
              "Department1"
            ],
            "IsManager": false
          }
        },
        "workflowData": {
          "currentStep": "HR Review",
          "attachmentsCount": 2
        }
      },
      "requestData": {
        "reason": {
          "name": "Vacation",
          "affectingBalance": true,
          "medicalCertificateRequired": false
        },
        "leavePeriod": {
          "start": "2024-09-06",
          "end": "2024-09-07"
        },
        "type": "Half Day",
        "traveledDays": 3
      }
    }
    """;

var jsonData = JsonNode.Parse(data);

var filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "schemas/mexico/absence/template.json");

var schemaRaw = JsonSchema.FromFile(filePath);

var result = schemaRaw.Evaluate(jsonData);
Console.WriteLine($"IsValid: {result.IsValid}");

Console.WriteLine(result.Details.Where(d => !d.IsValid).ToJsonDocument().RootElement.GetRawText());

Console.WriteLine(schemaRaw.Bundle().ToJsonDocument().RootElement.GetRawText());


static void RegisterGlobalSchemas()
{
    var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "schemas/common/");

    var files = Directory.GetFiles(path, "*.json");
    foreach (var file in files)
    {
        var schema = JsonSchema.FromFile(file);
        SchemaRegistry.Global.Register(schema);
    }
}