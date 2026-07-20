namespace StreamingApi.Exceptions;

/// <summary>
/// Resource yang diminta tidak ditemukan. Akan dipetakan ke HTTP 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string resource, object key)
        : base($"{resource} dengan id '{key}' tidak ditemukan") { }
}