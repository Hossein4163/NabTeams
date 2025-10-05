using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NabTeams.Application.Abstractions;

namespace NabTeams.Infrastructure.Services;

public class LocalRegistrationDocumentStorage : IRegistrationDocumentStorage
{
    private readonly IHostEnvironment _environment;

    public LocalRegistrationDocumentStorage(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<StoredRegistrationDocument> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        var uploadsRoot = Path.Combine(_environment.ContentRootPath, "wwwroot", "uploads", "registrations");
        Directory.CreateDirectory(uploadsRoot);

        var sanitized = SanitizeFileName(fileName);
        var uniqueFileName = $"{Path.GetFileNameWithoutExtension(sanitized)}-{Guid.NewGuid():N}{Path.GetExtension(sanitized)}";
        var destinationPath = Path.Combine(uploadsRoot, uniqueFileName);

        await using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        var relativeUrl = $"/uploads/registrations/{uniqueFileName}";
        return new StoredRegistrationDocument(uniqueFileName, relativeUrl);
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
