using NabTeams.Domain.Enums;

namespace NabTeams.Application.Abstractions;

public record ModerationResult(
    double RiskScore,
    IReadOnlyCollection<string> PolicyTags,
    ModerationDecision Decision,
    string Notes,
    int PenaltyPoints);

public enum ModerationDecision
{
    Publish,
    SoftWarn,
    Hold,
    Block,
    BlockAndReport
}

public record MessageCandidate(string UserId, RoleChannel Channel, string Content);

public interface IModerationService
{
    Task<ModerationResult> ModerateAsync(MessageCandidate candidate, CancellationToken cancellationToken = default);
}
