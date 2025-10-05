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

public class LocalRegistrationDocumentStorage : IRegistrationDocumentStorage
{
    private readonly IHostEnvironment _environment;
    private readonly string? _customRoot;
    private readonly string? _publicBaseUrl;

    public LocalRegistrationDocumentStorage(IHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _customRoot = configuration["Registration:StoragePath"];
        _publicBaseUrl = configuration["Registration:PublicBaseUrl"];
    }

    public async Task<StoredRegistrationDocument> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var uploadsRoot = ResolveUploadsRoot();
        Directory.CreateDirectory(uploadsRoot);

        var sanitized = SanitizeFileName(fileName);
        var uniqueFileName = $"{Path.GetFileNameWithoutExtension(sanitized)}-{Guid.NewGuid():N}{Path.GetExtension(sanitized)}";
        var destinationPath = Path.Combine(uploadsRoot, uniqueFileName);

        await using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        var relativeUrl = BuildRelativeUrl(uploadsRoot, uniqueFileName);
        return new StoredRegistrationDocument(uniqueFileName, relativeUrl);
    }

    private string ResolveUploadsRoot()
    {
        if (string.IsNullOrWhiteSpace(_customRoot))
        {
            return Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads", "registrations");
        }

        return Path.IsPathRooted(_customRoot)
            ? _customRoot
            : Path.Combine(_environment.ContentRootPath, _customRoot);
    }

    private string BuildRelativeUrl(string uploadsRoot, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(_publicBaseUrl))
        {
            return $"{_publicBaseUrl.TrimEnd('/')}/{fileName}";
        }

        var normalizedUploads = uploadsRoot.Replace('\\', '/');
        var uploadsSegment = normalizedUploads.Contains("/wwwroot/")
            ? normalizedUploads[(normalizedUploads.IndexOf("/wwwroot/", StringComparison.Ordinal) + "/wwwroot".Length)..]
            : "/uploads/registrations";

        if (!uploadsSegment.StartsWith('/'))
        {
            uploadsSegment = $"/{uploadsSegment}";
        }

        return $"{uploadsSegment.TrimEnd('/')}/{fileName}";
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "document";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(fileName.Length);
        foreach (var ch in fileName)
        {
            builder.Append(invalidChars.Contains(ch) ? '-' : ch);
        }

        var sanitized = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "document" : sanitized;
    }
}
