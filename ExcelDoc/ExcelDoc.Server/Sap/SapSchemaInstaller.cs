using System.Net;
using System.Text.Json;
using ExcelDoc.Server.Services.Interfaces;

namespace ExcelDoc.Server.Sap;

public sealed record SapSchemaInstallResult(
    int CreatedTables,
    int CreatedFields,
    int CreatedKeys);

public sealed class SapSchemaInstaller
{
    private readonly ISapServiceLayerClient _client;
    private readonly ISapSessionContextAccessor _sessionAccessor;

    public SapSchemaInstaller(
        ISapServiceLayerClient client,
        ISapSessionContextAccessor sessionAccessor)
    {
        _client = client;
        _sessionAccessor = sessionAccessor;
    }

    public async Task<SapSchemaInstallResult> EnsureCreatedAsync(
        CancellationToken cancellationToken = default)
    {
        var session = _sessionAccessor.GetRequiredSession();
        var createdTables = 0;
        var createdFields = 0;
        var createdKeys = 0;

        foreach (var table in SapUdtSchema.Tables)
        {
            if (!await TableExistsAsync(session, table.Name, cancellationToken))
            {
                await PostMetadataAsync(
                    session,
                    "UserTablesMD",
                    new
                    {
                        TableName = table.Name,
                        TableDescription = table.Description,
                        TableType = "bott_MasterData"
                    },
                    cancellationToken);
                createdTables++;
            }

            foreach (var field in table.Fields)
            {
                if (await FieldExistsAsync(
                        session,
                        table.Name,
                        field.Name,
                        cancellationToken))
                {
                    continue;
                }

                var payload = new Dictionary<string, object?>
                {
                    ["TableName"] = $"@{table.Name}",
                    ["Name"] = field.Name,
                    ["Description"] = field.Description,
                    ["Type"] = field.Type,
                    ["SubType"] = field.SubType ?? "st_None"
                };

                if (field.Size.HasValue)
                {
                    payload["Size"] = field.Size.Value;
                    payload["EditSize"] = field.Size.Value;
                }

                await PostMetadataAsync(
                    session,
                    "UserFieldsMD",
                    payload,
                    cancellationToken);
                createdFields++;
            }

            if (!await UserObjectExistsAsync(
                    session,
                    table.Name,
                    cancellationToken))
            {
                await PostMetadataAsync(
                    session,
                    "UserObjectsMD",
                    new
                    {
                        Code = table.Name,
                        Name = table.Description,
                        TableName = table.Name,
                        ObjectType = "boud_MasterData",
                        CanCancel = "tNO",
                        CanClose = "tNO",
                        CanCreateDefaultForm = "tNO",
                        CanDelete = "tYES",
                        CanFind = "tYES",
                        CanLog = "tNO",
                        CanYearTransfer = "tNO",
                        ManageSeries = "tNO",
                        ApplyAuthorization = "tNO"
                    },
                    cancellationToken);
            }

            foreach (var key in SapUdtSchema.GetKeys(table.Name))
            {
                if (await UserKeyExistsAsync(
                        session,
                        table.Name,
                        key.Name,
                        cancellationToken))
                {
                    continue;
                }

                await PostMetadataAsync(
                    session,
                    "UserKeysMD",
                    new
                    {
                        TableName = $"@{table.Name}",
                        KeyName = key.Name,
                        Unique = key.Unique ? "tYES" : "tNO",
                        UserKeysMD_Elements = key.Columns
                            .Select(column => new { ColumnAlias = column })
                            .ToArray()
                    },
                    cancellationToken);
                createdKeys++;
            }
        }

        return new SapSchemaInstallResult(
            createdTables,
            createdFields,
            createdKeys);
    }

    private async Task<bool> UserKeyExistsAsync(
        SapSessionContext session,
        string tableName,
        string keyName,
        CancellationToken cancellationToken)
    {
        var filter = SapOData.And(
            $"TableName eq {SapOData.String($"@{tableName}")}",
            $"KeyName eq {SapOData.String(keyName)}");
        var endpoint =
            $"UserKeysMD?$filter={Uri.EscapeDataString(filter)}&$select=KeyIndex&$top=1";

        using var response = await _client.SendAsync(
            session,
            HttpMethod.Get,
            endpoint,
            cancellationToken: cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, body);

        using var document = JsonDocument.Parse(body);
        return document.RootElement.TryGetProperty("value", out var value) &&
               value.ValueKind == JsonValueKind.Array &&
               value.GetArrayLength() > 0;
    }

    private async Task<bool> UserObjectExistsAsync(
        SapSessionContext session,
        string objectCode,
        CancellationToken cancellationToken)
    {
        using var response = await _client.SendAsync(
            session,
            HttpMethod.Get,
            $"UserObjectsMD({SapOData.String(objectCode)})?$select=Code",
            cancellationToken: cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        EnsureSuccess(response, body);
        return true;
    }

    private async Task<bool> TableExistsAsync(
        SapSessionContext session,
        string tableName,
        CancellationToken cancellationToken)
    {
        using var response = await _client.SendAsync(
            session,
            HttpMethod.Get,
            $"UserTablesMD({SapOData.String(tableName)})?$select=TableName",
            cancellationToken: cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        EnsureSuccess(response, body);
        return true;
    }

    private async Task<bool> FieldExistsAsync(
        SapSessionContext session,
        string tableName,
        string fieldName,
        CancellationToken cancellationToken)
    {
        var filter = SapOData.And(
            $"TableName eq {SapOData.String($"@{tableName}")}",
            $"Name eq {SapOData.String(fieldName)}");
        var endpoint =
            $"UserFieldsMD?$filter={Uri.EscapeDataString(filter)}&$select=FieldID&$top=1";

        using var response = await _client.SendAsync(
            session,
            HttpMethod.Get,
            endpoint,
            cancellationToken: cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        EnsureSuccess(response, body);

        using var document = JsonDocument.Parse(body);
        return document.RootElement.TryGetProperty("value", out var value) &&
               value.ValueKind == JsonValueKind.Array &&
               value.GetArrayLength() > 0;
    }

    private async Task PostMetadataAsync(
        SapSessionContext session,
        string endpoint,
        object payload,
        CancellationToken cancellationToken)
    {
        using var response = await _client.SendAsync(
            session,
            HttpMethod.Post,
            endpoint,
            payload,
            cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode || IsAlreadyExists(response.StatusCode, body))
        {
            return;
        }

        EnsureSuccess(response, body);
    }

    private static bool IsAlreadyExists(HttpStatusCode statusCode, string body) =>
        statusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict &&
        (body.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
         body.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
         body.Contains("-2035", StringComparison.Ordinal));

    private static void EnsureSuccess(
        HttpResponseMessage response,
        string responseBody)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new SapSessionExpiredException(
                SapUdtStore.ExtractMessage(responseBody) ??
                "A sessão do SAP Business One expirou. Entre novamente no sistema.");
        }

        throw new SapServiceLayerException(
            SapUdtStore.ExtractMessage(responseBody) ??
            $"Falha ao instalar metadados no SAP: {(int)response.StatusCode} ({response.StatusCode}).",
            response.StatusCode,
            responseBody);
    }
}
