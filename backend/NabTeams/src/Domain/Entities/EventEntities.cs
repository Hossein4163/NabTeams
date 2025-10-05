using System;
using System.Collections.Generic;
using NabTeams.Domain.Enums;

namespace NabTeams.Domain.Entities;

public record EventSummary
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
        = null;
    public DateTimeOffset? StartsAt { get; init; }
        = null;
    public DateTimeOffset? EndsAt { get; init; }
        = null;
    public bool AiTaskManagerEnabled { get; init; }
        = false;
}

public record EventDetail : EventSummary
{
    public IReadOnlyCollection<ParticipantTask> SampleTasks { get; init; }
        = Array.Empty<ParticipantTask>();
}

public record ParticipantTask
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ParticipantRegistrationId { get; init; }
        = Guid.Empty;
    public Guid EventId { get; init; }
        = Guid.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ParticipantTaskStatus Status { get; init; }
        = ParticipantTaskStatus.Todo;
    public DateTimeOffset CreatedAt { get; init; }
        = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; init; }
        = null;
    public DateTimeOffset? DueAt { get; init; }
        = null;
    public string? AssignedTo { get; init; }
        = null;
    public string? AiRecommendation { get; init; }
        = null;
}
