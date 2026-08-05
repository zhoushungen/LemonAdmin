using Lemon.Api.Authorization;
using Lemon.Application.Abstractions.Storage;
using Lemon.Application.Common;
using Lemon.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Lemon.Api.Controllers.Admin.V1;

[ApiController]
[Route("api/admin/v1/files")]
public sealed class FilesController : ControllerBase
{
    private const long MaxFileSize = 20L * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> AllowedTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            [".pdf"] = "application/pdf",
            [".txt"] = "text/plain",
            [".csv"] = "text/csv",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

    private readonly IObjectStorage _storage;
    public FilesController(IObjectStorage storage) => _storage = storage;

    [RequirePermission("system.file.upload")]
    [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
    [HttpPost("upload")]
    public async Task<ApiResponse<UploadResult>> Upload(IFormFile file, CancellationToken ct)
    {
        if (file.Length <= 0)
            throw new AppException("FILE_1001", "文件不能为空");
        if (file.Length > MaxFileSize)
            throw new AppException("FILE_1002", "文件不能超过 20MB", 413);

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedTypes.TryGetValue(extension, out var contentType))
            throw new AppException("FILE_1003", "不支持该文件类型，仅允许常用图片、PDF、TXT、CSV、XLSX 和 DOCX");

        var objectKey = $"system/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await using var stream = file.OpenReadStream();
        var result = await _storage.UploadAsync(stream, objectKey, contentType, ct);
        return ApiResponse<UploadResult>.Success(result, HttpContext.TraceIdentifier);
    }
}
