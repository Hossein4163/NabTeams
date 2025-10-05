using Microsoft.EntityFrameworkCore;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NabTeams.Infrastructure.Persistence;

public class ParticipantTaskRepository : IParticipantTaskRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ParticipantTaskRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ParticipantTask> CreateAsync(ParticipantTask task, CancellationToken cancellationToken = default)
    {
        var entity = task.ToEntity();
        entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
        entity.CreatedAt = DateTimeOffset.UtcNow;
        _dbContext.ParticipantTasks.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await LoadAsync(entity.Id, cancellationToken))!;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ParticipantTasks.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _dbContext.ParticipantTasks.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ParticipantTask?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await LoadAsync(id, cancellationToken);

    public async Task<IReadOnlyCollection<ParticipantTask>> ListForParticipantAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.ParticipantTasks
            .AsNoTracking()
            .Where(x => x.ParticipantRegistrationId == participantId)
            .OrderBy(x => x.DueAt ?? DateTimeOffset.MaxValue)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(x => x.ToModel()).ToList();
    }

    public async Task<ParticipantTask?> UpdateAsync(Guid id, ParticipantTask task, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ParticipantTasks.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Title = task.Title;
        entity.Description = task.Description;
        entity.DueAt = task.DueAt;
        entity.AssignedTo = task.AssignedTo;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadAsync(entity.Id, cancellationToken);
    }

    public async Task<ParticipantTask?> UpdateStatusAsync(Guid id, ParticipantTaskStatus status, string? aiRecommendation, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ParticipantTasks.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Status = status;
        entity.AiRecommendation = aiRecommendation;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadAsync(entity.Id, cancellationToken);
    }

    private async Task<ParticipantTask?> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ParticipantTasks
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity?.ToModel();
    }
}
