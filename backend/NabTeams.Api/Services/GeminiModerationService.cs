using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using NabTeams.Api.Models;

namespace NabTeams.Api.Services;

public class GeminiModerationService : IModerationService
{
    private static readonly IReadOnlyList<PolicyRule> Rules = new List<PolicyRule>
    {
        new("hate", "گروه", 0.75, 6),
        new("violence", "تهدید", 0.7, 5),
        new("spam", "اسپم", 0.55, 3),
        new("cheat", "تقلب", 0.65, 4),
        new("leak", "افشای اطلاعات", 0.8, 6),
        new("http://", "لینک", 0.4, 1),
        new("https://", "لینک", 0.4, 1),
        new("کدملی", "اطلاعات حساس", 0.85, 8),
        new("خودکشی", "خودآسیب", 0.9, 10)
    };

    private static readonly Regex ProfanityRegex = new("(?i)(لعنتی|احمق|stupid|idiot)", RegexOptions.Compiled);

    private static readonly ConcurrentDictionary<string, double> TrustAdjustments = new();

    public Task<ModerationResult> ModerateAsync(MessageCandidate candidate, CancellationToken cancellationToken = default)
    {
        var lowered = candidate.Content.ToLowerInvariant();
        var matchedTags = new HashSet<string>();
        double risk = 0.05; // base noise floor
        int penalty = 0;

        foreach (var rule in Rules)
        {
            if (!lowered.Contains(rule.Keyword))
            {
                continue;
            }

            matchedTags.Add(rule.PolicyTag);
            risk = Math.Max(risk, rule.RiskScore);
            penalty = Math.Max(penalty, rule.PenaltyPoints);
        }

        if (ProfanityRegex.IsMatch(candidate.Content))
        {
            matchedTags.Add("توهین");
            risk = Math.Max(risk, 0.6);
            penalty = Math.Max(penalty, 4);
        }

        if (candidate.Content.Length > 600)
        {
            matchedTags.Add("پیام طولانی");
            risk = Math.Max(risk, 0.3);
        }

        var trustAdjustment = TrustAdjustments.GetOrAdd(candidate.UserId, _ => 0);
        risk = Math.Clamp(risk + trustAdjustment, 0, 1);

        var decision = DetermineDecision(risk, matchedTags.Count);
        var notes = decision switch
        {
            ModerationDecision.Publish => "پیام سالم شناسایی شد.",
            ModerationDecision.SoftWarn => "پیام منتشر شد اما شامل علائم ریسک است.",
            ModerationDecision.Hold => "پیام برای بررسی انسانی نگه داشته شد.",
            ModerationDecision.Block => "پیام مسدود شد و به کاربر هشدار داده می‌شود.",
            ModerationDecision.BlockAndReport => "پیام مسدود و برای پیگیری ادمین پرچم شد.",
            _ => ""
        };

        if (decision is ModerationDecision.Publish or ModerationDecision.SoftWarn)
        {
            TrustAdjustments.AddOrUpdate(candidate.UserId, _ => -0.05, (_, current) => Math.Max(-0.1, current - 0.05));
        }
        else
        {
            TrustAdjustments.AddOrUpdate(candidate.UserId, _ => 0.1, (_, current) => Math.Min(0.2, current + 0.05));
        }

        penalty = decision switch
        {
            ModerationDecision.Publish => 0,
            ModerationDecision.SoftWarn => Math.Max(penalty, 0),
            ModerationDecision.Hold => Math.Max(penalty, 1),
            ModerationDecision.Block => Math.Max(penalty, 3),
            ModerationDecision.BlockAndReport => Math.Max(penalty, 5),
            _ => penalty
        };

        var result = new ModerationResult(
            Math.Round(risk, 2),
            matchedTags.ToList(),
            decision,
            notes,
            penalty);

        return Task.FromResult(result);
    }

    private static ModerationDecision DetermineDecision(double risk, int tagCount)
    {
        return risk switch
        {
            <= 0.2 => ModerationDecision.Publish,
            <= 0.4 => ModerationDecision.SoftWarn,
            <= 0.6 => ModerationDecision.Hold,
            <= 0.8 => ModerationDecision.Block,
            _ => ModerationDecision.BlockAndReport
        };
    }

    private sealed record PolicyRule(string Keyword, string PolicyTag, double RiskScore, int PenaltyPoints);
}
