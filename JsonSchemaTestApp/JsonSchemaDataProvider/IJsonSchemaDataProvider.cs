namespace JsonSchemaTestApp.JsonSchemaDataProvider;

public interface IJsonSchemaDataProvider
{
    Task<string> QueryDataAsync(string query, Dictionary<string, object?> variables, CancellationToken cancellationToken);
}
