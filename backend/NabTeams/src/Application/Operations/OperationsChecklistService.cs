using System;
using System.Linq;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Operations.Models;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Application.Operations;

public class OperationsChecklistService : IOperationsChecklistService
{
    private readonly IOperationsChecklistRepository _repository;

    public OperationsChecklistService(IOperationsChecklistRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<OperationsChecklistItemModel>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(cancellationToken);
        return items.Select(Map).OrderBy(item => item.Category).ThenBy(item => item.Title).ToList();
    }

    public async Task<OperationsChecklistItemModel> UpdateAsync(Guid id, OperationsChecklistUpdateModel update, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetAsync(id, cancellationToken);
        if (entity is null)
        {
            throw new InvalidOperationException($"Checklist item '{id}' not found.");
        }

        entity.Status = update.Status;
        entity.Notes = string.IsNullOrWhiteSpace(update.Notes) ? null : update.Notes.Trim();
        entity.ArtifactUrl = string.IsNullOrWhiteSpace(update.ArtifactUrl) ? null : update.ArtifactUrl.Trim();
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        entity.CompletedAt = update.Status == OperationsChecklistStatus.Completed ? entity.CompletedAt ?? DateTimeOffset.UtcNow : null;

        var saved = await _repository.UpdateAsync(entity, cancellationToken);
        return Map(saved);
    }

    private static OperationsChecklistItemModel Map(OperationsChecklistItemEntity entity) =>
        new(entity.Id, entity.Key, entity.Title, entity.Description, entity.Category, entity.Status, entity.CompletedAt, entity.Notes, entity.ArtifactUrl, entity.CreatedAt, entity.UpdatedAt);
}
