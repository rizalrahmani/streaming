namespace StreamingApi.Models.Dtos;

public class GoogleDriveStreamResult : IDisposable
{
    private readonly HttpResponseMessage? _response;

    public GoogleDriveStreamResult(HttpResponseMessage? response = null)
    {
        _response = response;
    }

    public required Stream Stream { get; init; }
    public required int StatusCode { get; init; }
    public required string ContentType { get; init; }
    public string? ContentRange { get; init; }

    public void Dispose()
    {
        Stream.Dispose();
        _response?.Dispose();
    }
}
