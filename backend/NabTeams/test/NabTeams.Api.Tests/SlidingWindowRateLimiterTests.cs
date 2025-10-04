using NabTeams.Application.Abstractions;
using NabTeams.Domain.Enums;
using NabTeams.Infrastructure.Services;
using Xunit;

namespace NabTeams.Api.Tests;

public class SlidingWindowRateLimiterTests
{
    [Fact]
    public void Allows_WithinConfiguredLimit()
    {
        var limiter = new SlidingWindowRateLimiter();

        RateLimitResult result = default!;
        for (var i = 0; i < 5; i++)
        {
            result = limiter.CheckQuota("user-1", RoleChannel.Participant);
            Assert.True(result.Allowed);
        }

        Assert.True(result.Allowed);
    }

    [Fact]
    public void Blocks_WhenLimitExceeded()
    {
        var limiter = new SlidingWindowRateLimiter();

        RateLimitResult lastResult = default!;
        for (var i = 0; i < 21; i++)
        {
            lastResult = limiter.CheckQuota("user-2", RoleChannel.Participant);
        }

        Assert.False(lastResult.Allowed);
        Assert.NotNull(lastResult.RetryAfter);
        Assert.Contains("نرخ", lastResult.Message, StringComparison.Ordinal);
    }
}
