using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NabTeams.Application.Abstractions;

public interface IFileStorageService
{
    Task<FileUploadResult> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        long length,
        CancellationToken cancellationToken = default);
}

public record FileUploadResult(
    string OriginalFileName,
    string StoredFileName,
    string FileUrl,
    string ContentType,
    long Length);
