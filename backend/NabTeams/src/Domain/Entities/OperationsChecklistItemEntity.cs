using NabTeams.Domain.Enums;

namespace NabTeams.Domain.Entities;

public class OperationsChecklistItemEntity
{
    public Guid Id { get; set; }
    public string Key { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Category { get; set; } = default!;
    public OperationsChecklistStatus Status { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public string? ArtifactUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
