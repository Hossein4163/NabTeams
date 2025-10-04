namespace NabTeams.Api.Models;

public enum AppealStatus
{
    Pending,
    Accepted,
    Rejected
}

public record Appeal
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid MessageId { get; init; }
    public RoleChannel Channel { get; init; }
    public string UserId { get; init; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; init; } = DateTimeOffset.UtcNow;
    public string Reason { get; init; } = string.Empty;
    public AppealStatus Status { get; init; } = AppealStatus.Pending;
    public string? ResolutionNotes { get; init; }
    public string? ReviewedBy { get; init; }
    public DateTimeOffset? ReviewedAt { get; init; }
}
