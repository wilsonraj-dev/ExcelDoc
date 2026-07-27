using System.Net;

namespace ExcelDoc.Server.Sap;

public sealed class SapServiceLayerException : HttpRequestException
{
    public SapServiceLayerException(
        string message,
        HttpStatusCode statusCode,
        string responseBody)
        : base(message, null, statusCode)
    {
        ResponseBody = responseBody;
    }

    public string ResponseBody { get; }
}
