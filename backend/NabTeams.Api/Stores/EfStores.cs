using Microsoft.EntityFrameworkCore;
using NabTeams.Api.Data;
using NabTeams.Api.Models;
using System.Linq;

namespace NabTeams.Api.Stores;

public class EfChatRepository : IChatRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EfChatRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddMessageAsync(Message message, CancellationToken cancellationToken = default)
    {
        var entity = message.ToEntity();
        _dbContext.Messages.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Message>> GetMessagesAsync(RoleChannel channel, CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.Messages
            .AsNoTracking()
            .Where(m => m.Channel == channel && m.Status != MessageStatus.Blocked)
            .OrderBy(m => m.CreatedAt)
            .Take(500)
            .ToListAsync(cancellationToken);

        return items.Select(m => m.ToModel()).ToList();
    }

    public async Task<Message?> GetMessageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Messages
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == id, cancellationToken);

        return entity?.ToModel();
    }

    public async Task UpdateMessageModerationAsync(
        Guid messageId,
        MessageStatus status,
        double risk,
        IReadOnlyCollection<string> tags,
        string? notes,
        int penaltyPoints,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Messages
            .SingleOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (entity is null)
        {
            return;
        }

        entity.Status = status;
        entity.ModerationRisk = risk;
        entity.ModerationTags = tags.ToList();
        entity.ModerationNotes = notes;
        entity.PenaltyPoints = penaltyPoints;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class EfModerationLogStore : IModerationLogStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfModerationLogStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ModerationLog log, CancellationToken cancellationToken = default)
    {
        _dbContext.ModerationLogs.Add(log.ToEntity());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ModerationLog>> QueryAsync(RoleChannel channel, CancellationToken cancellationToken = default)
    {
        var logs = await _dbContext.ModerationLogs
            .AsNoTracking()
            .Where(l => l.Channel == channel)
            .OrderByDescending(l => l.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return logs.Select(l => l.ToModel()).ToList();
    }

    public async Task<ModerationLog?> GetAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ModerationLogs
            .AsNoTracking()
            .SingleOrDefaultAsync(l => l.MessageId == messageId, cancellationToken);

        return entity?.ToModel();
    }
}

public class EfUserDisciplineStore : IUserDisciplineStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfUserDisciplineStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserDiscipline> UpdateScoreAsync(string userId, RoleChannel channel, int delta, string reason, Guid messageId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.UserDisciplines
            .Include(x => x.Events)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Channel == channel, cancellationToken);

        if (record is null)
        {
            record = new UserDisciplineEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Channel = channel,
                ScoreBalance = 0
            };
            _dbContext.UserDisciplines.Add(record);
        }

        record.ScoreBalance += delta;
        record.Events.Add(new DisciplineEventEntity
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            OccurredAt = DateTimeOffset.UtcNow,
            Delta = delta,
            Reason = reason,
            UserDisciplineId = record.Id
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetAsync(userId, channel, cancellationToken))!;
    }

    public async Task<UserDiscipline?> GetAsync(string userId, RoleChannel channel, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.UserDisciplines
            .AsNoTracking()
            .Include(x => x.Events)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Channel == channel, cancellationToken);

        return record?.ToModel();
    }
}

public class EfSupportKnowledgeBase : ISupportKnowledgeBase
{
    private readonly ApplicationDbContext _dbContext;

    public EfSupportKnowledgeBase(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<KnowledgeBaseItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.KnowledgeBaseItems
            .AsNoTracking()
            .OrderByDescending(i => i.UpdatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(i => i.ToModel()).ToList();
    }

    public async Task<KnowledgeBaseItem> UpsertAsync(KnowledgeBaseItem item, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.KnowledgeBaseItems
            .SingleOrDefaultAsync(i => i.Id == item.Id, cancellationToken);

        if (existing is null)
        {
            existing = item.ToEntity();
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            _dbContext.KnowledgeBaseItems.Add(existing);
        }
        else
        {
            existing.Title = item.Title;
            existing.Body = item.Body;
            existing.Audience = item.Audience;
            existing.Tags = item.Tags.ToList();
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing.ToModel();
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.KnowledgeBaseItems
            .SingleOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (existing is null)
        {
            return;
        }

        _dbContext.KnowledgeBaseItems.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class EfAppealStore : IAppealStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfAppealStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Appeal> CreateAsync(Appeal appeal, CancellationToken cancellationToken = default)
    {
        var entity = appeal.ToEntity();
        _dbContext.Appeals.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    public async Task<IReadOnlyCollection<Appeal>> GetForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var results = await _dbContext.Appeals
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync(cancellationToken);

        return results.Select(a => a.ToModel()).ToList();
    }

    public async Task<IReadOnlyCollection<Appeal>> QueryAsync(RoleChannel? channel, AppealStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Appeals.AsNoTracking().AsQueryable();

        if (channel.HasValue)
        {
            query = query.Where(a => a.Channel == channel.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        var results = await query
            .OrderBy(a => a.Status)
            .ThenByDescending(a => a.SubmittedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return results.Select(a => a.ToModel()).ToList();
    }

    public async Task<Appeal?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Appeals
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

        return entity?.ToModel();
    }

    public async Task<Appeal?> ResolveAsync(Guid id, AppealStatus status, string reviewerId, string? notes, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Appeals.SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Status = status;
        entity.ResolutionNotes = notes;
        entity.ReviewedBy = reviewerId;
        entity.ReviewedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
