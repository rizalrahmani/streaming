namespace StreamingApi.Exceptions;

/// <summary>
/// Validasi bisnis gagal (di luar DataAnnotations). Akan dipetakan ke HTTP 400.
/// </summary>
public class ValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(Dictionary<string, string[]> errors)
        : base("Satu atau lebih validasi gagal")
    {
        Errors = errors;
    }
}