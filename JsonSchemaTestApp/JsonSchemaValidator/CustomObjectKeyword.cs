using Json.Schema;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JsonSchemaTestApp.JsonSchemaValidator;

// The SchemaKeyword attribute is how the deserializer knows to use this
// class for the "maxDate" keyword.
[SchemaKeyword(Name)]
// Naturally, we want to be able to deserialize it.
[JsonConverter(typeof(CustomObjectKeywordJsonConverter))]
// We need to declare which vocabulary this keyword belongs to.
[Vocabulary("http://mydates.com/vocabulary")]
// Specify which versions the keyword is compatible with.
[SchemaSpecVersion(SpecVersion.Draft7)]
public class CustomObjectKeyword : IJsonSchemaKeyword
{
    // Define the keyword in one place.
    public const string Name = "customObject";

    // Define whatever data the keyword needs.
    public string customObjectValue { get; }

    public CustomObjectKeyword(string value)
    {
        customObjectValue = value;
    }

    public KeywordConstraint GetConstraint(SchemaConstraint schemaConstraint,
                                           ReadOnlySpan<KeywordConstraint> localConstraints,
                                           EvaluationContext context)
    {

        if (customObjectValue == "date-range")
        {
            return new KeywordConstraint(Name, DateRangeEvaluator);
        }
        else if (customObjectValue == "datetime-range")
        {
            return new KeywordConstraint(Name, DateTimeRangeEvaluator);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private void DateRangeEvaluator(KeywordEvaluation evaluation, EvaluationContext context)
    {
        var schemaValueType = evaluation.LocalInstance.GetSchemaValueType();
        
        if (schemaValueType is not (SchemaValueType.Object))
        {
            evaluation.MarkAsSkipped();
            return;
        }

        var range = evaluation.LocalInstance!.AsObject();

        var startValue = evaluation.LocalInstance["start"]!.AsValue().GetValue<string>();
        var endValue = evaluation.LocalInstance["end"]!.AsValue().GetValue<string>();

        var startDate = DateOnly.Parse(startValue);
        var endDate = DateOnly.Parse(endValue);

        if (startDate > endDate)
            evaluation.Results.Fail("start", 
                ErrorMessages.GetExclusiveMaximum(context.Options.Culture)
                .ReplaceToken("received", startValue)
                .ReplaceToken("limit", endValue));
    }

    private void DateTimeRangeEvaluator(KeywordEvaluation evaluation, EvaluationContext context)
    {
        var schemaValueType = evaluation.LocalInstance.GetSchemaValueType();
        if (schemaValueType is not (SchemaValueType.Object))
        {
            evaluation.MarkAsSkipped();
            return;
        }

        var range = evaluation.LocalInstance!.AsObject();

        var startValue = evaluation.LocalInstance["start"]!.AsValue().GetValue<string>();
        var endValue = evaluation.LocalInstance["end"]!.AsValue().GetValue<string>();

        var startDate = DateOnly.Parse(startValue);
        var endDate = DateOnly.Parse(endValue);

        if (startDate > endDate)
            evaluation.Results.Fail("start",
                ErrorMessages.GetExclusiveMaximum(context.Options.Culture)
                .ReplaceToken("received", startValue)
                .ReplaceToken("limit", endValue));
    }
}

class CustomObjectKeywordJsonConverter : JsonConverter<CustomObjectKeyword>
{
    public override CustomObjectKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Check to see if it's a string first.
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string");

        var dateString = reader.GetString();

        return new CustomObjectKeyword(dateString!);
    }

    public override void Write(Utf8JsonWriter writer, CustomObjectKeyword value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.customObjectValue);
    }
}