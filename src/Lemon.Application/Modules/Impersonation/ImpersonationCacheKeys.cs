namespace Lemon.Application.Modules.Impersonation;

public static class ImpersonationCacheKeys
{
    public static string Session(string sessionId) => $"impersonation:session:{sessionId}";
}
