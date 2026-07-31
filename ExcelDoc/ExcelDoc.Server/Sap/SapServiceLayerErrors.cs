using System.Net;
using System.Text.Json;
using B1SLayer;
using Flurl.Http;

namespace ExcelDoc.Server.Sap;

internal sealed record SapServiceLayerError(
    HttpStatusCode? StatusCode,
    string? ResponseBody,
    string Message);

internal static class SapServiceLayerErrors
{
    public static bool IsServiceLayerException(Exception exception) =>
        exception is SLException || FindFlurlException(exception) is not null;

    public static async Task<SapServiceLayerError> ReadAsync(Exception exception)
    {
        var flurlException = FindFlurlException(exception);
        var rawStatusCode = flurlException?.Call.Response?.StatusCode;
        var statusCode = rawStatusCode.HasValue
            ? (HttpStatusCode?)rawStatusCode.Value
            : null;
        string? responseBody = null;

        if (flurlException is not null)
        {
            try
            {
                responseBody = await flurlException.GetResponseStringAsync();
            }
            catch
            {
                // A mensagem tipada da B1SLayer permanece disponível na exceção.
            }
        }

        return new SapServiceLayerError(
            statusCode,
            responseBody,
            ExtractMessage(responseBody) ?? exception.Message);
    }

    public static Exception CreateException(
        SapServiceLayerError error,
        Exception innerException,
        string fallbackMessage,
        string sessionExpiredMessage)
    {
        if (error.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new SapSessionExpiredException(
                string.IsNullOrWhiteSpace(error.Message)
                    ? sessionExpiredMessage
                    : error.Message,
                innerException);
        }

        return new SapServiceLayerException(
            string.IsNullOrWhiteSpace(error.Message)
                ? fallbackMessage
                : error.Message,
            error.StatusCode ?? HttpStatusCode.InternalServerError,
            error.ResponseBody ?? string.Empty);
    }

    public static void UpdateSessionExpiration(
        SapSessionContext session,
        HttpStatusCode? statusCode)
    {
        if (statusCode == HttpStatusCode.Unauthorized)
        {
            session.ExpiresAtUtc = DateTime.UtcNow;
            return;
        }

        session.RenewExpiration();
    }

    public static string? ExtractMessage(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (!document.RootElement.TryGetProperty("error", out var error) ||
                !error.TryGetProperty("message", out var message))
            {
                return null;
            }

            if (message.ValueKind == JsonValueKind.String)
            {
                return message.GetString();
            }

            return message.TryGetProperty("value", out var value)
                ? value.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static FlurlHttpException? FindFlurlException(Exception exception)
    {
        if (exception is FlurlHttpException flurlException)
        {
            return flurlException;
        }

        if (exception is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.Flatten().InnerExceptions)
            {
                var match = FindFlurlException(innerException);
                if (match is not null)
                {
                    return match;
                }
            }
        }

        return exception.InnerException is null
            ? null
            : FindFlurlException(exception.InnerException);
    }
}
