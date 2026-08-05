namespace Lemon.Application.Modules.Impersonation;

public sealed record StartImpersonationRequest(long TargetAdminId, string Reason);
