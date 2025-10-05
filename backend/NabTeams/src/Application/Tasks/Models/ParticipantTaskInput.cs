using System;

namespace NabTeams.Application.Tasks.Models;

public record ParticipantTaskInput
{
    public Guid EventId { get; init; }
        = Guid.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTimeOffset? DueAt { get; init; }
        = null;
    public string? AssignedTo { get; init; }
        = null;
}
