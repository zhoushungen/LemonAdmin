namespace Lemon.Api.Contracts;

public sealed record ApiResponse<T>(string Code, string Message, T? Data, string? TraceId)
{
    public static ApiResponse<T> Success(T? data, string? traceId = null) =>
        new("0", "success", data, traceId);
}

public sealed record ApiResponse(string Code, string Message, object? Data, string? TraceId)
{
    public static ApiResponse Success(object? data = null, string? traceId = null) =>
        new("0", "success", data, traceId);

    public static ApiResponse Fail(string code, string message, string? traceId = null) =>
        new(code, message, null, traceId);
}
