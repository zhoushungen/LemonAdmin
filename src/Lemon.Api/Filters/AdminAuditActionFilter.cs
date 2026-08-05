using FluentValidation;
using System.Diagnostics;
using Lemon.Application.Abstractions.Repositories;
using Lemon.Application.Abstractions.Security;
using Lemon.Domain.System;
using Lemon.Application.Modules.Impersonation;
using Lemon.Application.Common;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lemon.Api.Filters;

public sealed class AdminAuditActionFilter : IAsyncActionFilter
{
    private readonly IAuditLogRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AdminAuditActionFilter> _logger;

    public AdminAuditActionFilter(IAuditLogRepository repository, ICurrentUser currentUser, ILogger<AdminAuditActionFilter> logger)
    {
        _repository = repository;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        ActionExecutedContext? executed = null;
        Exception? actionException = null;

        try
        {
            executed = await next();
            actionException = executed.Exception is not null && !executed.ExceptionHandled
                ? executed.Exception
                : null;
        }
        catch (Exception ex)
        {
            actionException = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            if (_currentUser.IsAuthenticated && !HttpMethods.IsGet(context.HttpContext.Request.Method))
            {
                try
                {
                    await _repository.WriteAsync(new AuditLog
                    {
                        AdminUserId = _currentUser.UserId,
                        DepartmentId = _currentUser.DepartmentId,
                        ActorAdminUserId = _currentUser.ActorUserId,
                        IsImpersonating = _currentUser.IsImpersonating,
                        Module = context.RouteData.Values["controller"]?.ToString() ?? "unknown",
                        Action = context.RouteData.Values["action"]?.ToString() ?? "unknown",
                        RequestPath = context.HttpContext.Request.Path,
                        HttpMethod = context.HttpContext.Request.Method,
                        RequestSummary = BuildSummary(context.ActionArguments),
                        StatusCode = ResolveStatusCode(
                            actionException,
                            executed?.HttpContext.Response.StatusCode ?? context.HttpContext.Response.StatusCode),
                        IpAddress = _currentUser.IpAddress,
                        UserAgent = context.HttpContext.Request.Headers.UserAgent.ToString(),
                        ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                        TraceId = context.HttpContext.TraceIdentifier
                    });
                }
                catch (Exception auditException)
                {
                    _logger.LogWarning(auditException, "写入审计日志失败");
                }
            }
        }
    }

    private static int ResolveStatusCode(Exception? exception, int currentStatusCode) => exception switch
    {
        AppException appException => appException.StatusCode,
        ValidationException => StatusCodes.Status400BadRequest,
        not null => StatusCodes.Status500InternalServerError,
        _ => currentStatusCode
    };

    private static string BuildSummary(IDictionary<string, object?> arguments)
    {
        var request = arguments.Values.OfType<StartImpersonationRequest>().FirstOrDefault();
        if (request is not null)
            return $"targetAdminId={request.TargetAdminId}; reason={request.Reason.Trim()}";
        return string.Join(',', arguments.Keys);
    }
}
