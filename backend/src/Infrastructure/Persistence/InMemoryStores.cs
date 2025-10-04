using System.Collections.Concurrent;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Infrastructure.Persistence;

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

    public Task<Message?> GetMessageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = _messages.Values.SelectMany(x => x).SingleOrDefault(m => m.Id == id);
        return Task.FromResult(message);
    }

    public Task UpdateMessageModerationAsync(Guid messageId, MessageStatus status, double risk, IReadOnlyCollection<string> tags, string? notes, int penaltyPoints, CancellationToken cancellationToken = default)
    {
        foreach (var list in _messages.Values)
        {
            lock (list)
            {
                var message = list.SingleOrDefault(m => m.Id == messageId);
                if (message is null)
                {
                    continue;
                }

                list[list.IndexOf(message)] = message with
                {
                    Status = status,
                    ModerationRisk = risk,
                    ModerationTags = tags.ToList(),
                    ModerationNotes = notes,
                    PenaltyPoints = penaltyPoints
                };
                break;
            }
        }

        return Task.CompletedTask;
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

    public Task<ModerationLog?> GetAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var log = _logs.FirstOrDefault(l => l.MessageId == messageId);
        return Task.FromResult(log);
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
