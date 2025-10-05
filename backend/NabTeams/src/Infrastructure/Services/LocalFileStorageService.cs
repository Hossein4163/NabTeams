using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NabTeams.Application.Abstractions;

namespace NabTeams.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly FileStorageOptions _options;
    private readonly string _rootPath;
    private readonly string _publicBaseUrl;

    public LocalFileStorageService(IOptions<FileStorageOptions> options, ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;
        _options = options.Value ?? new FileStorageOptions();
        _rootPath = ResolveRootPath(_options.RootPath);
        _publicBaseUrl = string.IsNullOrWhiteSpace(_options.PublicBaseUrl) ? "/uploads" : _options.PublicBaseUrl.TrimEnd('/');

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<FileUploadResult> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        long length,
        CancellationToken cancellationToken = default)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        var sanitizedOriginalName = SanitizeFileName(fileName);
        var extension = Path.GetExtension(sanitizedOriginalName);
        var baseName = Path.GetFileNameWithoutExtension(sanitizedOriginalName);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "document";
        }

        var storedFileName = $"{baseName}-{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(_rootPath, storedFileName);

        await using (var target = File.Create(destinationPath))
        {
            await content.CopyToAsync(target, cancellationToken);
        }

        _logger.LogInformation(
            "Stored registration document {FileName} ({Length} bytes) at {Path}",
            sanitizedOriginalName,
            length,
            destinationPath);

        var fileUrl = CombineUrl(_publicBaseUrl, storedFileName);

        return new FileUploadResult(
            sanitizedOriginalName,
            storedFileName,
            fileUrl,
            contentType,
            length);
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = string.IsNullOrWhiteSpace(fileName) ? "document" : fileName.Trim();
        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            builder.Append(invalidChars.Contains(ch) ? '-' : ch);
        }

        return builder.ToString();
    }

    private static string ResolveRootPath(string? configuredRoot)
    {
        if (string.IsNullOrWhiteSpace(configuredRoot))
        {
            return Path.Combine(AppContext.BaseDirectory, "storage", "uploads");
        }

        var path = configuredRoot;
        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, path);
        }

        return Path.GetFullPath(path);
    }

    private static string CombineUrl(string baseUrl, string fileName)
    {
        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out var absolute))
        {
            return new Uri(absolute, fileName).ToString();
        }

        if (!baseUrl.StartsWith('/'))
        {
            baseUrl = $"/{baseUrl}";
        }

        return $"{baseUrl}/{fileName}";
    }
}
