using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Application.Abstractions;

public interface IChatRepository
{
    Task AddMessageAsync(Message message, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Message>> GetMessagesAsync(RoleChannel channel, CancellationToken cancellationToken = default);
    Task<Message?> GetMessageAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateMessageModerationAsync(
        Guid messageId,
        MessageStatus status,
        double risk,
        IReadOnlyCollection<string> tags,
        string? notes,
        int penaltyPoints,
        CancellationToken cancellationToken = default);
}

public interface IModerationLogStore
{
    Task AddAsync(ModerationLog log, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ModerationLog>> QueryAsync(RoleChannel channel, CancellationToken cancellationToken = default);
    Task<ModerationLog?> GetAsync(Guid messageId, CancellationToken cancellationToken = default);
}

public interface IUserDisciplineStore
{
    Task<UserDiscipline> UpdateScoreAsync(string userId, RoleChannel channel, int delta, string reason, Guid messageId, CancellationToken cancellationToken = default);
    Task<UserDiscipline?> GetAsync(string userId, RoleChannel channel, CancellationToken cancellationToken = default);
}

public interface IAppealStore
{
    Task<Appeal> CreateAsync(Appeal appeal, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Appeal>> GetForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Appeal>> QueryAsync(RoleChannel? channel, AppealStatus? status, CancellationToken cancellationToken = default);
    Task<Appeal?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Appeal?> ResolveAsync(Guid id, AppealStatus status, string reviewerId, string? notes, CancellationToken cancellationToken = default);
}
