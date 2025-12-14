using System.Net;

namespace DesktopRFID.Data.Services;

public sealed class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? RawBody { get; }
    public string UserMessage { get; }

    public ApiException(HttpStatusCode statusCode, string userMessage, string? rawBody = null)
        : base(userMessage)
    {
        StatusCode = statusCode;
        RawBody = rawBody;
        UserMessage = userMessage;
    }
}