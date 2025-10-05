using System.Collections.Generic;

namespace NabTeams.Application.Tasks.Models;

public record TaskAdviceResult
{
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyCollection<string> SuggestedTasks { get; init; }
        = Array.Empty<string>();
    public string? Risks { get; init; }
        = null;
    public string? NextSteps { get; init; }
        = null;
    public string RawResponse { get; init; } = string.Empty;
}
