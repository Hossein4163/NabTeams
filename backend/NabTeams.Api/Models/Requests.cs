namespace NabTeams.Api.Models;

public record SendMessageRequest
{
    public string Content { get; init; } = string.Empty;
}

public record SendMessageResponse
{
    public Guid MessageId { get; init; }
    public MessageStatus Status { get; init; }
    public double ModerationRisk { get; init; }
    public IReadOnlyCollection<string> ModerationTags { get; init; } = Array.Empty<string>();
    public string? ModerationNotes { get; init; }
    public int PenaltyPoints { get; init; }
    public bool SoftWarn { get; init; }
    public string? RateLimitMessage { get; init; }
}

public record MessagesResponse
{
    public IReadOnlyCollection<Message> Messages { get; init; } = Array.Empty<Message>();
}

public record KnowledgeBaseUpsertRequest
{
    public string? Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public IReadOnlyCollection<string>? Tags { get; init; }
}

public record CreateAppealRequest
{
    public Guid MessageId { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public record AppealDecisionRequest
{
    public AppealStatus Status { get; init; }
    public string? Notes { get; init; }
}
