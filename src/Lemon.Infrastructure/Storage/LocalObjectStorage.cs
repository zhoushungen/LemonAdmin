using Lemon.Application.Abstractions.Storage;
using Lemon.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Lemon.Infrastructure.Storage;

public sealed class LocalObjectStorage : IObjectStorage
{
    private readonly StorageOptions _options;
    public LocalObjectStorage(IOptions<StorageOptions> options) => _options = options.Value;

    public async Task<UploadResult> UploadAsync(
        Stream stream,
        string objectName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var safeName = NormalizeObjectName(objectName);
        var fullPath = ResolveSafePath(safeName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var output = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(output, cancellationToken);
        var url = string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? null
            : $"{_options.PublicBaseUrl.TrimEnd('/')}/{safeName}";
        return new UploadResult("Local", safeName, output.Length, contentType, url);
    }

    public Task DeleteAsync(string objectName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolveSafePath(NormalizeObjectName(objectName));
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<string> GetTemporaryUrlAsync(
        string objectName,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var safeName = NormalizeObjectName(objectName);
        var url = string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? safeName
            : $"{_options.PublicBaseUrl.TrimEnd('/')}/{safeName}";
        return Task.FromResult(url);
    }

    private string ResolveSafePath(string objectName)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(_options.LocalRoot));
        var fullPath = Path.GetFullPath(Path.Combine(root, objectName));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var rootPrefix = root + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootPrefix, comparison))
            throw new InvalidOperationException("非法文件路径");
        return fullPath;
    }

    private static string NormalizeObjectName(string objectName)
    {
        var value = objectName.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(value) || value.Split('/').Any(segment => segment is "." or ".."))
            throw new InvalidOperationException("非法对象名称");
        return value;
    }
}
