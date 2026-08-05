using Lemon.Application.Common;
using Lemon.Api.Contracts;
using FluentValidation;

namespace Lemon.Api.Middlewares;

public sealed class GlobalExceptionMiddleware
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
        try { await _next(context); }
        catch (ValidationException ex)
        {
            await WriteAsync(context, 400, ErrorCodes.ValidationFailed, string.Join("；", ex.Errors.Select(x => x.ErrorMessage)));
        }
        catch (AppException ex)
        {
            await WriteAsync(context, ex.StatusCode, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未处理异常，TraceId={TraceId}", context.TraceIdentifier);
            await WriteAsync(context, 500, ErrorCodes.InternalError, "服务器内部错误");
        }
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, string code, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(ApiResponse.Fail(code, message, context.TraceIdentifier));
    }
}
