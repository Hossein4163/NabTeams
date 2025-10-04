using NabTeams.Api.Models;

namespace NabTeams.Api.Services;

public record RateLimitResult(bool Allowed, TimeSpan? RetryAfter, string? Message);

public interface IRateLimiter
{
    RateLimitResult CheckQuota(string userId, RoleChannel channel);
}
