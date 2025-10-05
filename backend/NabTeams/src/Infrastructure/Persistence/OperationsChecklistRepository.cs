using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;

namespace NabTeams.Infrastructure.Persistence;

public class EfOperationsChecklistRepository : IOperationsChecklistRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EfOperationsChecklistRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OperationsChecklistItemEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.OperationsChecklistItems
            .AsNoTracking()
            .OrderBy(item => item.Category)
            .ThenBy(item => item.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationsChecklistItemEntity?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OperationsChecklistItems.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<OperationsChecklistItemEntity> UpdateAsync(OperationsChecklistItemEntity entity, CancellationToken cancellationToken = default)
    {
        _dbContext.OperationsChecklistItems.Update(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }
}
