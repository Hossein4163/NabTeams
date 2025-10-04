using System.Collections.Concurrent;
using NabTeams.Api.Models;

namespace NabTeams.Api.Stores;

public class InMemoryChatRepository : IChatRepository
{
    private readonly ConcurrentDictionary<RoleChannel, List<Message>> _messages = new();

    public Task AddMessageAsync(Message message, CancellationToken cancellationToken = default)
    {
        var list = _messages.GetOrAdd(message.Channel, _ => new List<Message>());
        lock (list)
        {
            list.Add(message);
            if (list.Count > 500)
            {
                list.RemoveRange(0, list.Count - 500);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<Message>> GetMessagesAsync(RoleChannel channel, CancellationToken cancellationToken = default)
    {
        var list = _messages.GetOrAdd(channel, _ => new List<Message>());
        lock (list)
        {
            return Task.FromResult((IReadOnlyCollection<Message>)list.OrderBy(m => m.CreatedAt).ToList());
        }
    }
}

public class InMemoryModerationLogStore : IModerationLogStore
{
    private readonly ConcurrentBag<ModerationLog> _logs = new();

    public Task AddAsync(ModerationLog log, CancellationToken cancellationToken = default)
    {
        _logs.Add(log);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<ModerationLog>> QueryAsync(RoleChannel channel, CancellationToken cancellationToken = default)
    {
        var results = _logs.Where(l => l.Channel == channel)
            .OrderByDescending(l => l.CreatedAt)
            .Take(100)
            .ToList();
        return Task.FromResult((IReadOnlyCollection<ModerationLog>)results);
    }
}

public class InMemoryUserDisciplineStore : IUserDisciplineStore
{
    private readonly ConcurrentDictionary<(string UserId, RoleChannel Channel), UserDiscipline> _users = new();

    public Task<UserDiscipline> UpdateScoreAsync(string userId, RoleChannel channel, int delta, string reason, Guid messageId, CancellationToken cancellationToken = default)
    {
        var key = (userId, channel);
        var record = _users.GetOrAdd(key, _ => new UserDiscipline
        {
            UserId = userId,
            Channel = channel
        });

        lock (record)
        {
            record.ScoreBalance += delta;
            record.History.Add(new DisciplineEvent
            {
                Delta = delta,
                MessageId = messageId,
                Reason = reason,
                OccurredAt = DateTimeOffset.UtcNow
            });
        }

        return Task.FromResult(record);
    }

    public Task<UserDiscipline?> GetAsync(string userId, RoleChannel channel, CancellationToken cancellationToken = default)
    {
        var key = (userId, channel);
        return Task.FromResult(_users.TryGetValue(key, out var record) ? record : null);
    }
}
