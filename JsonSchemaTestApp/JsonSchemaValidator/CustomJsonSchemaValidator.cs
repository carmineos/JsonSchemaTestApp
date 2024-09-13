using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Validation;

namespace JsonSchemaTestApp.JsonSchemaValidator;

public class CustomJsonSchemaValidator : NJsonSchema.Validation.JsonSchemaValidator, IJsonSchemaValidator
{
    protected override ICollection<ValidationError> Validate(JToken token, JsonSchema schema, SchemaType schemaType, string? propertyName, string propertyPath)
    {
        ICollection<ValidationError> errors = [];

        var baseValidationErrors = base.Validate(token, schema, schemaType, propertyName, propertyPath);

        ValidateDateRange(token, schema, schemaType, propertyName, propertyPath, errors);
        ValidateDateTimeRange(token, schema, schemaType, propertyName, propertyPath, errors);

        return baseValidationErrors.Concat(errors).ToList();
    }

    private const string CustomObjectKey = "customObject";
    private const string DateRangeKey = "date-range";
    private const string DateTimeRangeKey = "date-time-range";
    private const string StartPropertyKey = "start";
    private const string EndPropertyKey = "end";

    private bool IsCustomObject(JsonSchema schema, string customObjectValue)
    {
        if (schema.ExtensionData is null)
            return false;

        if (!schema.ExtensionData.TryGetValue(CustomObjectKey, out object? customObject))
            return false;

        if (customObject is null)
            return false;

        if (customObject is not string value)
            return false;

        return value == customObjectValue;
    }

    private void ValidateDateRange(JToken token, JsonSchema schema, SchemaType schemaType, string? propertyName, string propertyPath, ICollection<ValidationError> errors)
    {
        if (!IsCustomObject(schema, DateRangeKey))
            return;

        if (token is not JObject jObject)
            return;

        var start = GetPropertyValueByName<DateTime>(jObject, StartPropertyKey);

        if (start is null)
            errors.Add(new ValidationError(ValidationErrorKind.DateExpected, StartPropertyKey, propertyPath, token, schema));

        var end = GetPropertyValueByName<DateTime>(jObject, EndPropertyKey);

        if (end is null)
            errors.Add(new ValidationError(ValidationErrorKind.DateExpected, EndPropertyKey, propertyPath, token, schema));

        if (start is not null && end is not null && DateOnly.FromDateTime(start.Value) > DateOnly.FromDateTime(end.Value))
            errors.Add(new ValidationError(ValidationErrorKind.Unknown, StartPropertyKey, propertyPath, token, schema));
    }

    private void ValidateDateTimeRange(JToken token, JsonSchema schema, SchemaType schemaType, string? propertyName, string propertyPath, ICollection<ValidationError> errors)
    {
        if (!IsCustomObject(schema, DateTimeRangeKey))
            return;

        if (token is not JObject jObject)
            return;

        var start = GetPropertyValueByName<DateTime>(jObject, StartPropertyKey);

        if (start is null)
            errors.Add(new ValidationError(ValidationErrorKind.DateTimeExpected, StartPropertyKey, propertyPath, token, schema));

        var end = GetPropertyValueByName<DateTime>(jObject, EndPropertyKey);

        if (end is null)
            errors.Add(new ValidationError(ValidationErrorKind.DateTimeExpected, EndPropertyKey, propertyPath, token, schema));

        if (start is not null && end is not null && start.Value > end.Value)
            errors.Add(new ValidationError(ValidationErrorKind.Unknown, StartPropertyKey, propertyPath, token, schema));
    }

    private T? GetPropertyValueByName<T>(JObject obj, string name) where T : struct
    {
        var token = obj[name];

        if (token is not JValue { Type: JTokenType.String } value)
            return null;

        return value.Value<T>();
    }
}