using System;

namespace NabTeams.Domain.Entities;

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public string ActorId { get; set; } = default!;
    public string ActorName { get; set; } = default!;
    public string Action { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public string? Metadata { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
