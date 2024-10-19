using HotChocolate;
using HotChocolate.Execution;

namespace JsonSchemaTestApp.JsonSchemaDataProvider;

public class GraphQLJsonSchemaDataProvider : IJsonSchemaDataProvider
{
    private readonly IRequestExecutorResolver _requestResolver;

    public GraphQLJsonSchemaDataProvider(IRequestExecutorResolver requestResolver)
    {
        _requestResolver = requestResolver;
    }

    public async Task<string> QueryDataAsync(string query, Dictionary<string, object?> variables, CancellationToken cancellationToken)
    {
        var executor = await _requestResolver.GetRequestExecutorAsync(cancellationToken: cancellationToken);

        var result = await executor.ExecuteAsync(query, variables, cancellationToken);

        return result.ToJson();
    }
}

public class MyQueries
{
    private static readonly List<AbsenceReason> _absenceReasons =
        [
            new AbsenceReason { CompanyId = 3, Name= "Sick Leave", AffectingBalance= false, MedicalCertificateRequired = true },
            new AbsenceReason { CompanyId = 3, Name= "Vacation", AffectingBalance= true, MedicalCertificateRequired = false }
        ];

    public IQueryable<AbsenceReason> AbsenceReasons(int companyId)
         => _absenceReasons.Where(a => a.CompanyId == companyId).AsQueryable();
}

public class AbsenceReason
{
    public int CompanyId { get; set; }
    public string Name { get; set; } = default!;
    public bool AffectingBalance { get; set; }
    public bool MedicalCertificateRequired { get; set; }
}