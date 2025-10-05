using Microsoft.EntityFrameworkCore;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NabTeams.Infrastructure.Persistence;

public class EventRepository : IEventRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EventRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EventDetail> CreateAsync(EventDetail model, CancellationToken cancellationToken = default)
    {
        var entity = model.ToEntity();
        entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
        entity.CreatedAt = DateTimeOffset.UtcNow;
        _dbContext.Events.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadAsync(entity.Id, cancellationToken) ?? throw new InvalidOperationException("Event creation failed to persist.");
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Events.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _dbContext.Events.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<EventDetail?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await LoadAsync(id, cancellationToken);

    public async Task<IReadOnlyCollection<EventDetail>> ListAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Events
            .AsNoTracking()
            .Include(x => x.Tasks)
            .OrderByDescending(x => x.StartsAt ?? x.CreatedAt)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(x => x.ToDetail()).ToList();
    }

    public async Task<EventDetail?> UpdateAsync(EventDetail model, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Events.SingleOrDefaultAsync(x => x.Id == model.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Name = model.Name;
        entity.Description = model.Description;
        entity.StartsAt = model.StartsAt;
        entity.EndsAt = model.EndsAt;
        entity.AiTaskManagerEnabled = model.AiTaskManagerEnabled;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadAsync(entity.Id, cancellationToken);
    }

    private async Task<EventDetail?> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Events
            .AsNoTracking()
            .Include(x => x.Tasks)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity?.ToDetail();
    }
}
