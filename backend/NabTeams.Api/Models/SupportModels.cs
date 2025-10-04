namespace NabTeams.Api.Models;

public record SupportQuery
{
    public string Question { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

public record SupportAnswer
{
    public string Answer { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Sources { get; init; } = Array.Empty<string>();
    public double Confidence { get; init; }
    public bool EscalateToHuman { get; init; }
}

public record KnowledgeBaseItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string Audience { get; init; } = "all";
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
