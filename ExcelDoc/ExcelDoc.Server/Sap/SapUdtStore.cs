using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.Json;
using B1SLayer;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Sap;

public sealed class SapUdtStore : ISapUdtStore
{
    private const int CodeLength = 10;
    private const int MaxInsertAttempts = 3;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> CodeLocks =
        new(StringComparer.Ordinal);
    private static readonly JsonSerializerOptions SapJsonOptions = new()
    {
        PropertyNamingPolicy = null,
        DictionaryKeyPolicy = null
    };

    private readonly ISapSessionContextAccessor _sessionAccessor;

    public SapUdtStore(ISapSessionContextAccessor sessionAccessor)
    {
        _sessionAccessor = sessionAccessor;
    }

    public async Task<IReadOnlyList<SapUdtRecord>> QueryAsync(
        string tableName,
        string? filter = null,
        string? orderBy = null,
        int? top = null,
        int? skip = null,
        string? select = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = BuildQueryEndpoint(
            SapUdtSchema.Endpoint(tableName),
            filter,
            orderBy,
            top,
            skip,
            select);
        var session = _sessionAccessor.GetRequiredSession();
        var records = new List<SapUdtRecord>();

        while (!string.IsNullOrWhiteSpace(endpoint))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string body;
            try
            {
                body = await session
                    .GetRequiredConnection()
                    .Request(endpoint)
                    .WithTimeout(session.RequestTimeoutSeconds)
                    .GetStringAsync();
                session.RenewExpiration();
            }
            catch (Exception exception) when (
                SapServiceLayerErrors.IsServiceLayerException(exception))
            {
                throw await TranslateExceptionAsync(session, exception);
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                break;
            }

            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.TryGetProperty("value", out var value) &&
                value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in value.EnumerateArray())
                {
                    records.Add(new SapUdtRecord(item));
                    if (top.HasValue && records.Count >= top.Value)
                    {
                        return records;
                    }
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                records.Add(new SapUdtRecord(root));
            }

            endpoint = NormalizeNextLink(GetNextLink(root), session);
        }

        return records;
    }

    public async Task<SapUdtRecord?> GetByIdAsync(
        string tableName,
        int id,
        CancellationToken cancellationToken = default)
    {
        var session = _sessionAccessor.GetRequiredSession();
        cancellationToken.ThrowIfCancellationRequested();
        string body;
        try
        {
            body = await session
                .GetRequiredConnection()
                .Request(EntityEndpoint(tableName, id))
                .WithTimeout(session.RequestTimeoutSeconds)
                .GetStringAsync();
            session.RenewExpiration();
        }
        catch (Exception exception) when (
            SapServiceLayerErrors.IsServiceLayerException(exception))
        {
            var error = await SapServiceLayerErrors.ReadAsync(exception);
            SapServiceLayerErrors.UpdateSessionExpiration(session, error.StatusCode);
            if (error.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            throw CreateException(error, exception);
        }

        using var document = JsonDocument.Parse(body);
        return new SapUdtRecord(document.RootElement);
    }

    public async Task<int> CountAsync(
        string tableName,
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"{SapUdtSchema.Endpoint(tableName)}/$count";
        if (!string.IsNullOrWhiteSpace(filter))
        {
            endpoint += $"?$filter={Uri.EscapeDataString(filter)}";
        }

        var session = _sessionAccessor.GetRequiredSession();
        cancellationToken.ThrowIfCancellationRequested();
        string body;
        try
        {
            body = await session
                .GetRequiredConnection()
                .Request(endpoint)
                .WithTimeout(session.RequestTimeoutSeconds)
                .GetStringAsync();
            session.RenewExpiration();
        }
        catch (Exception exception) when (
            SapServiceLayerErrors.IsServiceLayerException(exception))
        {
            throw await TranslateExceptionAsync(session, exception);
        }

        return int.TryParse(
            body.Trim().Trim('"'),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var count)
            ? count
            : throw new InvalidOperationException(
                "O SAP Service Layer retornou uma contagem inválida.");
    }

    public async Task<int> AddAsync(
        string tableName,
        IReadOnlyDictionary<string, object?> fields,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        var session = _sessionAccessor.GetRequiredSession();
        var lockKey = $"{session.Database}\n{tableName}";
        var codeLock = CodeLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

        await codeLock.WaitAsync(cancellationToken);
        try
        {
            var nextId = await GetNextIdAsync(tableName, cancellationToken);
            for (var attempt = 0; attempt < MaxInsertAttempts; attempt++, nextId++)
            {
                var code = FormatCode(nextId);
                var payload = BuildPayload(fields);
                payload["Code"] = code;
                payload["Name"] = string.IsNullOrWhiteSpace(name) ? code : name;

                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await session
                        .GetRequiredConnection()
                        .Request(SapUdtSchema.Endpoint(tableName))
                        .WithJsonSerializerOptions(SapJsonOptions)
                        .WithTimeout(session.RequestTimeoutSeconds)
                        .PostAsync(payload);
                    session.RenewExpiration();
                    return nextId;
                }
                catch (Exception exception) when (
                    SapServiceLayerErrors.IsServiceLayerException(exception))
                {
                    var error = await SapServiceLayerErrors.ReadAsync(exception);
                    SapServiceLayerErrors.UpdateSessionExpiration(session, error.StatusCode);
                    var errorText = $"{error.ResponseBody}\n{error.Message}";
                    if (attempt + 1 < MaxInsertAttempts &&
                        IsDuplicate(error.StatusCode, errorText))
                    {
                        continue;
                    }

                    throw CreateException(error, exception);
                }
            }

            throw new InvalidOperationException(
                $"Nao foi possivel reservar um Code para a tabela {tableName}.");
        }
        finally
        {
            codeLock.Release();
        }
    }

    public async Task UpdateAsync(
        string tableName,
        int id,
        IReadOnlyDictionary<string, object?> fields,
        CancellationToken cancellationToken = default)
    {
        var session = _sessionAccessor.GetRequiredSession();
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await session
                .GetRequiredConnection()
                .Request(EntityEndpoint(tableName, id))
                .WithJsonSerializerOptions(SapJsonOptions)
                .WithTimeout(session.RequestTimeoutSeconds)
                .PatchAsync(BuildPayload(fields));
            session.RenewExpiration();
        }
        catch (Exception exception) when (
            SapServiceLayerErrors.IsServiceLayerException(exception))
        {
            throw await TranslateExceptionAsync(session, exception);
        }
    }

    public async Task DeleteAsync(
        string tableName,
        int id,
        CancellationToken cancellationToken = default)
    {
        var session = _sessionAccessor.GetRequiredSession();
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await session
                .GetRequiredConnection()
                .Request(EntityEndpoint(tableName, id))
                .WithTimeout(session.RequestTimeoutSeconds)
                .DeleteAsync();
            session.RenewExpiration();
        }
        catch (Exception exception) when (
            SapServiceLayerErrors.IsServiceLayerException(exception))
        {
            var error = await SapServiceLayerErrors.ReadAsync(exception);
            SapServiceLayerErrors.UpdateSessionExpiration(session, error.StatusCode);
            if (error.StatusCode == HttpStatusCode.NotFound)
            {
                return;
            }

            throw CreateException(error, exception);
        }
    }

    private async Task<int> GetNextIdAsync(
        string tableName,
        CancellationToken cancellationToken)
    {
        var latest = await QueryAsync(
            tableName,
            orderBy: "Code desc",
            top: 1,
            select: "Code",
            cancellationToken: cancellationToken);

        return latest.Count == 0
            ? 1
            : checked(latest[0].Id + 1);
    }

    private static Dictionary<string, object?> BuildPayload(
        IReadOnlyDictionary<string, object?> fields)
    {
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (name, value) in fields)
        {
            payload[SapUdtSchema.Field(name)] = value;
        }

        return payload;
    }

    private static string EntityEndpoint(string tableName, int id) =>
        $"{SapUdtSchema.Endpoint(tableName)}({SapOData.String(FormatCode(id))})";

    private static string FormatCode(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id));
        }

        return id.ToString($"D{CodeLength}", CultureInfo.InvariantCulture);
    }

    private static string BuildQueryEndpoint(
        string endpoint,
        string? filter,
        string? orderBy,
        int? top,
        int? skip,
        string? select)
    {
        var query = new List<string>();
        AddQuery(query, "$filter", filter);
        AddQuery(query, "$orderby", orderBy);
        AddQuery(query, "$select", select);

        if (top.HasValue)
        {
            query.Add($"$top={top.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        if (skip.HasValue)
        {
            query.Add($"$skip={skip.Value.ToString(CultureInfo.InvariantCulture)}");
        }

        return query.Count == 0 ? endpoint : $"{endpoint}?{string.Join("&", query)}";
    }

    private static void AddQuery(
        ICollection<string> query,
        string name,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            query.Add($"{name}={Uri.EscapeDataString(value)}");
        }
    }

    private static string? GetNextLink(JsonElement root)
    {
        foreach (var propertyName in new[]
                 {
                     "odata.nextLink",
                     "@odata.nextLink",
                     "nextLink"
                 })
        {
            if (root.TryGetProperty(propertyName, out var nextLink))
            {
                return nextLink.GetString();
            }
        }

        return null;
    }

    private static string? NormalizeNextLink(
        string? nextLink,
        SapSessionContext session)
    {
        if (string.IsNullOrWhiteSpace(nextLink))
        {
            return nextLink;
        }

        if (!Uri.TryCreate(session.ServiceLayerBaseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException(
                "A URL base da sessão SAP é inválida.");
        }

        var normalized = nextLink.Trim();
        if (Uri.TryCreate(normalized, UriKind.Absolute, out var absoluteNextLink))
        {
            if (!HasSameOrigin(baseUri, absoluteNextLink))
            {
                throw new InvalidOperationException(
                    "A paginação do SAP retornou uma URL fora da Service Layer configurada.");
            }

            return StripServicePath(baseUri, absoluteNextLink);
        }

        var servicePath = baseUri.AbsolutePath.Trim('/');
        var withoutLeadingSlash = normalized.TrimStart('/');
        var prefix = $"{servicePath}/";
        if (withoutLeadingSlash.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return withoutLeadingSlash[prefix.Length..];
        }

        if (normalized.StartsWith('/'))
        {
            throw new InvalidOperationException(
                "A paginação do SAP retornou um caminho fora da versão configurada da Service Layer.");
        }

        return normalized;
    }

    private static bool HasSameOrigin(Uri expected, Uri actual) =>
        string.Equals(expected.Scheme, actual.Scheme, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(expected.Host, actual.Host, StringComparison.OrdinalIgnoreCase) &&
        expected.Port == actual.Port;

    private static string StripServicePath(Uri baseUri, Uri nextLink)
    {
        var servicePath = baseUri.AbsolutePath.TrimEnd('/');
        if (!nextLink.AbsolutePath.StartsWith(
                $"{servicePath}/",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "A paginação do SAP retornou um caminho fora da versão configurada da Service Layer.");
        }

        return nextLink.PathAndQuery[(servicePath.Length + 1)..];
    }

    private static bool IsDuplicate(HttpStatusCode? statusCode, string responseBody)
    {
        return statusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict &&
               (responseBody.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                responseBody.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                responseBody.Contains("-2035", StringComparison.Ordinal));
    }

    private static async Task<Exception> TranslateExceptionAsync(
        SapSessionContext session,
        Exception exception)
    {
        var error = await SapServiceLayerErrors.ReadAsync(exception);
        SapServiceLayerErrors.UpdateSessionExpiration(session, error.StatusCode);
        return CreateException(error, exception);
    }

    private static Exception CreateException(
        SapServiceLayerError error,
        Exception exception) =>
        SapServiceLayerErrors.CreateException(
            error,
            exception,
            "O SAP Service Layer não conseguiu concluir a operação solicitada.",
            "A sessão do SAP Business One expirou. Entre novamente no sistema.");
}
