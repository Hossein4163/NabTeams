namespace NabTeams.Api.Models;

public record SendMessageRequest
{
    public string UserId { get; init; } = string.Empty;
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
