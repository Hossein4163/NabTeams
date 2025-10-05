using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Infrastructure.Persistence;

public class EfIntegrationSettingsRepository : IIntegrationSettingsRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EfIntegrationSettingsRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<IntegrationSetting>> ListAsync(
        IntegrationProviderType? type,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.IntegrationSettings.AsNoTracking();
        if (type.HasValue)
        {
            query = query.Where(s => s.Type == type.Value);
        }

        var items = await query
            .OrderByDescending(s => s.IsActive)
            .ThenBy(s => s.DisplayName)
            .ToListAsync(cancellationToken);

        return items.Select(s => s.ToModel()).ToList();
    }

    public async Task<IntegrationSetting?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.IntegrationSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

        return entity?.ToModel();
    }

    public async Task<IntegrationSetting?> GetActiveAsync(
        IntegrationProviderType type,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.IntegrationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Type == type && s.IsActive, cancellationToken);

        return entity?.ToModel();
    }

    public async Task<IntegrationSetting> UpsertAsync(
        IntegrationSetting setting,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.IntegrationSettings
            .SingleOrDefaultAsync(s => s.Id == setting.Id, cancellationToken);

        if (entity is null)
        {
            entity = setting.ToEntity();
            _dbContext.IntegrationSettings.Add(entity);
        }
        else
        {
            entity.Type = setting.Type;
            entity.ProviderKey = setting.ProviderKey;
            entity.DisplayName = setting.DisplayName;
            entity.Configuration = setting.Configuration;
            entity.IsActive = setting.IsActive;
            entity.CreatedAt = setting.CreatedAt;
            entity.UpdatedAt = setting.UpdatedAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    public async Task SetActiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var target = await _dbContext.IntegrationSettings
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (target is null)
        {
            return;
        }

        var sameType = await _dbContext.IntegrationSettings
            .Where(s => s.Type == target.Type)
            .ToListAsync(cancellationToken);

        foreach (var setting in sameType)
        {
            setting.IsActive = setting.Id == id;
            setting.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.IntegrationSettings
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (entity is null)
        {
            return;
        }

        _dbContext.IntegrationSettings.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
