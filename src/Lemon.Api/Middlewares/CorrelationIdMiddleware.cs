namespace Lemon.Api.Middlewares;

public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-ID";
    private const int MaxLength = 64;
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var requestedId = context.Request.Headers[HeaderName].FirstOrDefault()?.Trim();
        var correlationId = IsValid(requestedId) ? requestedId! : context.TraceIdentifier;

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        await _next(context);
    }

    private static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= MaxLength &&
        value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.' or ':');
}
