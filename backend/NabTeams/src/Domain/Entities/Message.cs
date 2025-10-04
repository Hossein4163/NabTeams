using NabTeams.Domain.Enums;

namespace NabTeams.Domain.Entities;

public enum MessageStatus
{
    Published,
    Held,
    Blocked
}

public record Message
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public RoleChannel Channel { get; init; }
    public string SenderUserId { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public MessageStatus Status { get; init; }
    public double ModerationRisk { get; init; }
    public IReadOnlyCollection<string> ModerationTags { get; init; } = Array.Empty<string>();
    public string? ModerationNotes { get; init; }
    public int PenaltyPoints { get; init; }
};

public record ModerationLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid MessageId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public RoleChannel Channel { get; init; }
    public double RiskScore { get; init; }
    public IReadOnlyCollection<string> PolicyTags { get; init; } = Array.Empty<string>();
    public string ActionTaken { get; init; } = string.Empty;
    public int PenaltyPoints { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
};

public record UserDiscipline
{
    public string UserId { get; init; } = string.Empty;
    public RoleChannel Channel { get; init; }
    public int ScoreBalance { get; set; }
    public List<DisciplineEvent> History { get; } = new();
}

public record DisciplineEvent
{
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public string Reason { get; init; } = string.Empty;
    public int Delta { get; init; }
    public Guid MessageId { get; init; }
}
