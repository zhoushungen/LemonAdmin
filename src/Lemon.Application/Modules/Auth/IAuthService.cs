namespace Lemon.Application.Modules.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(AdminLoginRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task RevokeAllAsync(long adminUserId, CancellationToken cancellationToken = default);
}
