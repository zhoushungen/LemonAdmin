namespace Lemon.Application.Abstractions.Storage;

public sealed record UploadResult(string Provider, string ObjectKey, long Size, string ContentType, string? Url = null);

public interface IObjectStorage
{
    Task<UploadResult> UploadAsync(Stream stream, string objectName, string contentType, CancellationToken cancellationToken = default);
    Task DeleteAsync(string objectName, CancellationToken cancellationToken = default);
    Task<string> GetTemporaryUrlAsync(string objectName, TimeSpan expiration, CancellationToken cancellationToken = default);
}
