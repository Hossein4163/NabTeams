using System.Collections.Concurrent;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Enums;

namespace NabTeams.Infrastructure.Services;

public class SlidingWindowRateLimiter : IRateLimiter
{
    private record RateLimitConfig(int MaxMessages, TimeSpan Window);

    private readonly ConcurrentDictionary<(string UserId, RoleChannel Channel), Queue<DateTimeOffset>> _entries = new();
    private readonly IReadOnlyDictionary<RoleChannel, RateLimitConfig> _configs = new Dictionary<RoleChannel, RateLimitConfig>
    {
        { RoleChannel.Participant, new RateLimitConfig(20, TimeSpan.FromMinutes(5)) },
        { RoleChannel.Judge, new RateLimitConfig(30, TimeSpan.FromMinutes(5)) },
        { RoleChannel.Mentor, new RateLimitConfig(25, TimeSpan.FromMinutes(5)) },
        { RoleChannel.Investor, new RateLimitConfig(15, TimeSpan.FromMinutes(5)) },
        { RoleChannel.Admin, new RateLimitConfig(40, TimeSpan.FromMinutes(5)) }
    };

    public RateLimitResult CheckQuota(string userId, RoleChannel channel)
    {
        var config = _configs[channel];
        var now = DateTimeOffset.UtcNow;
        var key = (userId, channel);
        var queue = _entries.GetOrAdd(key, _ => new Queue<DateTimeOffset>());

        while (queue.Count > 0 && now - queue.Peek() > config.Window)
        {
            queue.Dequeue();
        }

        if (queue.Count >= config.MaxMessages)
        {
            var retryAfter = config.Window - (now - queue.Peek());
            return new RateLimitResult(false, retryAfter, $"نرخ ارسال پیام شما محدود شده است. لطفاً {retryAfter.TotalSeconds:N0} ثانیه صبر کنید.");
        }

        queue.Enqueue(now);
        return new RateLimitResult(true, null, null);
    }
}
