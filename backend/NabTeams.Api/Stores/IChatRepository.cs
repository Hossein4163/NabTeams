using NabTeams.Api.Models;

namespace NabTeams.Api.Stores;

public interface IChatRepository
{
    Task AddMessageAsync(Message message, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Message>> GetMessagesAsync(RoleChannel channel, CancellationToken cancellationToken = default);
}

public interface IModerationLogStore
{
    Task AddAsync(ModerationLog log, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ModerationLog>> QueryAsync(RoleChannel channel, CancellationToken cancellationToken = default);
}

public interface IUserDisciplineStore
{
    Task<UserDiscipline> UpdateScoreAsync(string userId, RoleChannel channel, int delta, string reason, Guid messageId, CancellationToken cancellationToken = default);
    Task<UserDiscipline?> GetAsync(string userId, RoleChannel channel, CancellationToken cancellationToken = default);
}
