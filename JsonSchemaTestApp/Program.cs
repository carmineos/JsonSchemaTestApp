using JsonSchemaTestApp.JsonSchemaBuilder;
using JsonSchemaTestApp.JsonSchemaDataProvider;
using JsonSchemaTestApp.JsonSchemaLoader;
using JsonSchemaTestApp.JsonSchemaValidator;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

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
          "currentStep": "Draft",
          "attachmentsCount": 2
        }
      },
      "requestData": {
        "reason": {
          "name": "Sick Leave",
          "affectingBalance": false,
          "medicalCertificateRequired": true
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

var templateFile = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Schemas/mexico/absence/template.json"));

var builtSchema = await jsonSchemaBuilder.BuildAsync(templateFile, null, cancellationToken);

var errors = jsonSchemaValidator.Validate(data, builtSchema);

foreach (var error in errors)
{
    Console.WriteLine(error.ToString());
}

// Attempt to Draft4 -> Draft7
var patched = builtSchema.ToJson().Replace("readonly", "readOnly");

Console.WriteLine(patched);