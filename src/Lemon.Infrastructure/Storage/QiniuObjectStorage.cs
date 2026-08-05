using Lemon.Application.Abstractions.Storage;
using Lemon.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Qiniu.Http;
using Qiniu.Storage;
using Qiniu.Util;

namespace Lemon.Infrastructure.Storage;

public sealed class QiniuObjectStorage : IObjectStorage
{
    private readonly StorageOptions _options;
    private QiniuOptions Qiniu => _options.Qiniu;

    public QiniuObjectStorage(IOptions<StorageOptions> options) => _options = options.Value;

    public async Task<UploadResult> UploadAsync(
        Stream stream,
        string objectName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();
        objectName = NormalizeObjectName(objectName);

        await using var copy = new MemoryStream();
        await stream.CopyToAsync(copy, cancellationToken);
        var bytes = copy.ToArray();

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var mac = new Mac(Qiniu.AccessKey, Qiniu.SecretKey);
            var policy = new PutPolicy { Scope = $"{Qiniu.Bucket}:{objectName}" };
            policy.SetExpires(3600);

            var token = Auth.CreateUploadToken(mac, policy.ToJsonString());
            var uploader = new FormUploader(CreateConfig());
            var result = uploader.UploadData(bytes, objectName, token, new PutExtra
            {
                MimeType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
            });

            if (result.Code != (int)HttpCode.OK)
                throw new InvalidOperationException($"七牛云上传失败: {result.Code} {result.Text}");

            return new UploadResult(
                "Qiniu",
                objectName,
                bytes.LongLength,
                contentType,
                CreateUrl(objectName, TimeSpan.FromHours(1)));
        }, cancellationToken);
    }

    public Task DeleteAsync(string objectName, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();
        objectName = NormalizeObjectName(objectName);

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var manager = new BucketManager(
                new Mac(Qiniu.AccessKey, Qiniu.SecretKey),
                CreateConfig());
            var result = manager.Delete(Qiniu.Bucket, objectName);
            if (result.Code != (int)HttpCode.OK && result.Code != 612)
                throw new InvalidOperationException($"七牛云删除失败: {result.Code} {result.Text}");
        }, cancellationToken);
    }

    public Task<string> GetTemporaryUrlAsync(
        string objectName,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateConfiguration();
        return Task.FromResult(CreateUrl(NormalizeObjectName(objectName), expiration));
    }

    private string CreateUrl(string objectName, TimeSpan expiration)
    {
        var domain = Qiniu.Domain.TrimEnd('/');
        return Qiniu.PrivateBucket
            ? DownloadManager.CreatePrivateUrl(
                new Mac(Qiniu.AccessKey, Qiniu.SecretKey),
                domain,
                objectName,
                Math.Max(60, (int)expiration.TotalSeconds))
            : DownloadManager.CreatePublishUrl(domain, objectName);
    }

    private Config CreateConfig() => new()
    {
        Zone = Qiniu.Zone.ToLowerInvariant() switch
        {
            "cn-east-2" => Zone.ZONE_CN_East_2,
            "cn-north" => Zone.ZONE_CN_North,
            "cn-south" => Zone.ZONE_CN_South,
            "us-north" => Zone.ZONE_US_North,
            "asia-singapore" => Zone.ZONE_AS_Singapore,
            _ => Zone.ZONE_CN_East
        },
        UseHttps = Qiniu.UseHttps,
        UseCdnDomains = true,
        ChunkSize = ChunkUnit.U512K
    };

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(Qiniu.AccessKey) ||
            string.IsNullOrWhiteSpace(Qiniu.SecretKey) ||
            string.IsNullOrWhiteSpace(Qiniu.Bucket) ||
            string.IsNullOrWhiteSpace(Qiniu.Domain))
            throw new InvalidOperationException("Storage:Qiniu 配置不完整");
    }

    private static string NormalizeObjectName(string objectName)
    {
        var value = objectName.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(value) || value.Split('/').Any(segment => segment is "." or ".."))
            throw new InvalidOperationException("非法对象名称");
        return value;
    }
}
