namespace Lemon.Application.Common;

public static class ErrorCodes
{
    public const string ValidationFailed = "SYS_0001";
    public const string InternalError = "SYS_0002";
    public const string Unauthorized = "AUTH_1001";
    public const string Forbidden = "AUTH_1002";
    public const string InvalidCredentials = "AUTH_1003";
    public const string TokenExpired = "AUTH_1004";
    public const string AdminNotFound = "SYS_1001";
    public const string AdminDisabled = "SYS_1002";
    public const string DuplicateData = "SYS_1003";
    public const string NotFound = "SYS_1004";
}
