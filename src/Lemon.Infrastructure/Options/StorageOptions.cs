namespace Lemon.Infrastructure.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "Local";
    public string LocalRoot { get; set; } = "uploads";
    public string PublicBaseUrl { get; set; } = "http://localhost:5080/uploads";
    public QiniuOptions Qiniu { get; set; } = new();
}

public sealed class QiniuOptions
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Zone { get; set; } = "cn-east";
    public bool PrivateBucket { get; set; } = true;
    public bool UseHttps { get; set; } = true;
}
