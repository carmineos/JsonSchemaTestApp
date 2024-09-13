using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Validation;

namespace JsonSchemaTestApp.JsonSchemaValidator;

public interface IJsonSchemaValidator
{
    ICollection<ValidationError> Validate(string jsonData, JsonSchema schema, SchemaType schemaType = SchemaType.JsonSchema);

    ICollection<ValidationError> Validate(JToken token, JsonSchema schema, SchemaType schemaType = SchemaType.JsonSchema);
}
