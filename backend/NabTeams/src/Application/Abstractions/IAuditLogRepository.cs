using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NabTeams.Domain.Entities;

namespace NabTeams.Application.Abstractions;

public interface IAuditLogRepository
{
    Task<AuditLogEntry> AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLogEntry>> ListAsync(
        string? entityType = null,
        string? entityId = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default);
}
