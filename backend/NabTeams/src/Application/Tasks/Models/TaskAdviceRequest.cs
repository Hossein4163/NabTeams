using System;
using System.Collections.Generic;

namespace NabTeams.Application.Tasks.Models;

public record TaskAdviceRequest
{
    public Guid ParticipantRegistrationId { get; init; }
        = Guid.Empty;
    public Guid EventId { get; init; }
        = Guid.Empty;
    public string TaskContext { get; init; } = string.Empty;
    public string? FocusArea { get; init; }
        = null;
    public IReadOnlyCollection<string> ExistingTasks { get; init; }
        = Array.Empty<string>();
}
