using System.Text.Json;
using StreamingApi.Exceptions;

namespace StreamingApi.Middleware;

/// <summary>
/// Menangkap semua exception yang tidak tertangani, memetakan ke ProblemDetails (RFC 7807),
/// dan mengirim ke ILogger. Menggantikan try-catch di tiap controller.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (statusCode, title, errors) = ex switch
        {
            NotFoundException nf => (StatusCodes.Status404NotFound, "Resource tidak ditemukan", (Dictionary<string, string[]>?)null),
            ValidationException ve => (StatusCodes.Status400BadRequest, "Validasi gagal", ve.Errors),
            ExternalServiceException es => (StatusCodes.Status502BadGateway, $"Service eksternal '{es.Service}' gagal", null),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Tidak diotorisasi", null),
            _ => (StatusCodes.Status500InternalServerError, "Terjadi kesalahan internal server", null)
        };

        // Log dengan level berbeda berdasarkan severity
        if (statusCode >= 500)
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning(ex, "Client error on {Method} {Path}: {Message}", context.Request.Method, context.Request.Path, ex.Message);

        var problem = new
        {
            type = $"https://httpstatuses.com/{statusCode}",
            title,
            status = statusCode,
            detail = IsDev ? ex.Message : null,  // Sembunyikan detail di production
            traceId = context.TraceIdentifier,
            errors
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }

    private static bool IsDev =>
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
}