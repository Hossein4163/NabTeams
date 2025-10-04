using NabTeams.Domain.Enums;

namespace NabTeams.Application.Abstractions;

public record RateLimitResult(bool Allowed, TimeSpan? RetryAfter, string? Message);

public interface IRateLimiter
{
    RateLimitResult CheckQuota(string userId, RoleChannel channel);
}
