using Lemon.Application.Modules.Auth;

namespace Lemon.Application.Modules.Impersonation;

public interface IImpersonationService
{
    Task<AuthResponse> StartAsync(long actorAdminId, StartImpersonationRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> StopAsync(long actorAdminId, string sessionId, string? ipAddress, CancellationToken cancellationToken = default);
    Task CancelSessionAsync(string? sessionId, CancellationToken cancellationToken = default);
}
