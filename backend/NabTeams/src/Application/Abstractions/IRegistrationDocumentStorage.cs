using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NabTeams.Application.Abstractions;

public record StoredRegistrationDocument(string FileName, string FileUrl);

public interface IRegistrationDocumentStorage
{
    Task<StoredRegistrationDocument> SaveAsync(string fileName, Stream content, CancellationToken cancellationToken = default);
}
