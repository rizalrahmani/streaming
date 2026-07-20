namespace StreamingApi.Exceptions;

/// <summary>
/// External service (Google Drive, dll) gagal. Akan dipetakan ke HTTP 502/503.
/// </summary>
public class ExternalServiceException : Exception
{
    public string Service { get; }

    public ExternalServiceException(string service, string message, Exception? inner = null)
        : base($"[{service}] {message}", inner)
    {
        Service = service;
    }
}