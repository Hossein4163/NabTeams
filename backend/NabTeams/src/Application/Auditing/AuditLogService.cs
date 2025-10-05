using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;

namespace NabTeams.Application.Auditing;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;

    public AuditLogService(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<AuditLogEntry> LogAsync(
        string actorId,
        string actorName,
        string action,
        string entityType,
        string entityId,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorId = string.IsNullOrWhiteSpace(actorId) ? "system" : actorId,
            ActorName = string.IsNullOrWhiteSpace(actorName) ? actorId : actorName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Metadata = metadata is null ? null : JsonSerializer.Serialize(metadata),
            CreatedAt = DateTimeOffset.UtcNow
        };

        return await _repository.AddAsync(entry, cancellationToken);
    }

    public Task<IReadOnlyList<AuditLogEntry>> ListAsync(
        string? entityType = null,
        string? entityId = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
        => _repository.ListAsync(entityType, entityId, skip, take, cancellationToken);
}
