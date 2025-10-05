using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NabTeams.Application.Abstractions;

namespace NabTeams.Infrastructure.Services;

public class LocalOperationsArtifactStorage : IOperationsArtifactStorage
{
    private readonly IHostEnvironment _environment;
    private readonly string? _customRoot;
    private readonly string? _publicBaseUrl;

    public LocalOperationsArtifactStorage(IHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _customRoot = configuration["Operations:ArtifactsPath"];
        _publicBaseUrl = configuration["Operations:ArtifactsPublicBaseUrl"];
    }

    public async Task<StoredOperationsArtifact> SaveAsync(string itemKey, string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(itemKey))
        {
            itemKey = "operations-item";
        }

        var uploadsRoot = ResolveUploadsRoot();
        var itemFolder = Path.Combine(uploadsRoot, SanitizeSegment(itemKey));
        Directory.CreateDirectory(itemFolder);

        var sanitizedFile = SanitizeFileName(fileName);
        var uniqueFileName = $"{Path.GetFileNameWithoutExtension(sanitizedFile)}-{Guid.NewGuid():N}{Path.GetExtension(sanitizedFile)}";
        var destinationPath = Path.Combine(itemFolder, uniqueFileName);

        await using (var destination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await content.CopyToAsync(destination, cancellationToken);
        }

        var relativeUrl = BuildRelativeUrl(itemFolder, uniqueFileName);
        return new StoredOperationsArtifact(uniqueFileName, relativeUrl);
    }

    private string ResolveUploadsRoot()
    {
        if (string.IsNullOrWhiteSpace(_customRoot))
        {
            return Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads", "operations");
        }

        return Path.IsPathRooted(_customRoot)
            ? _customRoot
            : Path.Combine(_environment.ContentRootPath, _customRoot);
    }

    private string BuildRelativeUrl(string folderPath, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(_publicBaseUrl))
        {
            return $"{_publicBaseUrl.TrimEnd('/')}/{fileName}";
        }

        var uploadsRoot = ResolveUploadsRoot();
        var normalizedRoot = uploadsRoot.Replace('\\', '/');
        var normalizedFolder = folderPath.Replace('\\', '/');
        var index = normalizedFolder.IndexOf(normalizedRoot, StringComparison.Ordinal);
        var relativeFolder = index >= 0
            ? normalizedFolder[(index + normalizedRoot.Length)..].Trim('/')
            : string.Empty;

        var basePath = string.IsNullOrWhiteSpace(relativeFolder)
            ? "/uploads/operations"
            : $"/uploads/operations/{relativeFolder}";

        return $"{basePath.TrimEnd('/')}/{fileName}".Replace("//", "/");
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "artifact";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(fileName.Length);
        foreach (var ch in fileName)
        {
            builder.Append(invalidChars.Contains(ch) ? '-' : ch);
        }

        var sanitized = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "artifact" : sanitized;
    }

    private static string SanitizeSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
        {
            return "item";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(segment.Length);
        foreach (var ch in segment)
        {
            builder.Append(invalidChars.Contains(ch) ? '-' : ch);
        }

        var sanitized = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "item" : sanitized.ToLowerInvariant();
    }
}
