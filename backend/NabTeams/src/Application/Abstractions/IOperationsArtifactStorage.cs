using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NabTeams.Application.Abstractions;

public interface IOperationsArtifactStorage
{
    Task<StoredOperationsArtifact> SaveAsync(string itemKey, string fileName, Stream content, CancellationToken cancellationToken = default);
}

public record StoredOperationsArtifact(string FileName, string FileUrl);
