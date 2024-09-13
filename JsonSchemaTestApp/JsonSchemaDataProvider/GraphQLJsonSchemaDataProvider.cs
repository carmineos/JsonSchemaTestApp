using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace JsonSchemaTestApp.JsonSchemaDataProvider;

//public class GraphQLJsonSchemaDataProvider : IJsonSchemaDataProvider
//{
//    private readonly IRequestExecutorResolver _requestResolver;

//    public GraphQLJsonSchemaDataProvider(IRequestExecutorResolver requestResolver)
//    {
//        _requestResolver = requestResolver;
//    }

//    public async Task<string> QueryDataAsync(string query, Dictionary<string, object> variables, CancellationToken cancellationToken)
//    {
//        var executor = await _requestResolver.GetRequestExecutorAsync(cancellationToken: cancellationToken);

//        var result = await executor.ExecuteAsync(query, variables, cancellationToken);

//        return result.ToJson();
//    }
//}

public class MockJsonSchemaDataProvider : IJsonSchemaDataProvider
{
    public Task<string> QueryDataAsync(string query, Dictionary<string, object> variables, CancellationToken cancellationToken)
    {
        var result = 
            """
            {
              "data": {
                "absenceReasons": [
                  {
                    "name": "Sick Leave",
                    "affectingBalance": false,
                    "medicalCertificateRequired": true
                  },
                  {
                    "name": "Vacation",
                    "affectingBalance": true,
                    "medicalCertificateRequired": false
                  }
                ]
              }
            }
            """;

        return Task.FromResult(result);
    }


    //<PackageReference Include = "GraphQL.Client" Version="6.1.0" />
    //<PackageReference Include = "GraphQL.Client.Serializer.SystemTextJson" Version="6.1.0" />
    //private async Task<JObject> ExecuteGraphQLQueryAsync(string query, string endpoint)
    //{
    //    var graphQLClient = new GraphQLHttpClient(endpoint, new SystemTextJsonSerializer());
    //    var request = new GraphQLRequest { Query = query };
    //    var response = await graphQLClient.SendQueryAsync<object>(request);
    //    var element = (JsonElement)response.Data;
    //    return JObject.Parse(element.GetRawText());
    //}
}