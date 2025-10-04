using NabTeams.Domain.Entities;

namespace NabTeams.Application.Abstractions;

public interface ISupportKnowledgeBase
{
    Task<IReadOnlyCollection<KnowledgeBaseItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<KnowledgeBaseItem> UpsertAsync(KnowledgeBaseItem item, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public interface ISupportResponder
{
    Task<SupportAnswer> GetAnswerAsync(SupportQuery query, CancellationToken cancellationToken = default);
}
