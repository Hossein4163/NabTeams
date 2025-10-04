using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Common;

namespace NabTeams.Infrastructure.Services;

public class GeminiModerationService : IModerationService
{
    private static readonly IReadOnlyList<PolicyRule> Rules = new List<PolicyRule>
    {
        new("hate", "نفرت‌پراکنی", 0.75, 6),
        new("violence", "خشونت", 0.7, 5),
        new("spam", "اسپم", 0.55, 3),
        new("cheat", "تقلب", 0.65, 4),
        new("leak", "افشای اطلاعات", 0.8, 6),
        new("http://", "لینک", 0.4, 1),
        new("https://", "لینک", 0.4, 1),
        new("کدملی", "اطلاعات حساس", 0.85, 8),
        new("self harm", "خودآسیب", 0.9, 10)
    };

    private static readonly Regex ProfanityRegex = new("(?i)(لعنتی|احمق|stupid|idiot)", RegexOptions.Compiled);
    private static readonly ConcurrentDictionary<string, double> TrustAdjustments = new();

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<GeminiOptions> _options;
    private readonly ILogger<GeminiModerationService> _logger;

    public GeminiModerationService(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<GeminiOptions> options,
        ILogger<GeminiModerationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<ModerationResult> ModerateAsync(MessageCandidate candidate, CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        if (options.Enabled)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("gemini");
                var prompt = BuildModerationPrompt(candidate);
                var request = new GeminiGenerateRequest(options.ModerationModel, options.ApiKey, prompt);
                var httpRequest = request.ToHttpRequest();

                var response = await client.SendAsync(httpRequest, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                response.EnsureSuccessStatusCode();

                if (TryParseModeration(body, out var result))
                {
                    UpdateTrust(candidate.UserId, result.Decision);
                    var notes = string.IsNullOrWhiteSpace(result.Notes)
                        ? "نتیجه بررسی توسط Gemini."
                        : result.Notes;
                    return new ModerationResult(result.RiskScore, result.PolicyTags, result.Decision, notes, result.PenaltyPoints);
                }

                _logger.LogWarning("Gemini moderation response was not parsable: {Body}", body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini moderation failed, reverting to rule-based fallback");
            }
        }

        return RuleBasedModeration(candidate);
    }

    private static ModerationResult RuleBasedModeration(MessageCandidate candidate)
    {
        var lowered = candidate.Content.ToLowerInvariant();
        var matchedTags = new HashSet<string>();
        double risk = 0.05;
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

        var decision = DetermineDecision(risk);
        var notes = decision switch
        {
            ModerationDecision.Publish => "پیام سالم شناسایی شد.",
            ModerationDecision.SoftWarn => "پیام منتشر شد اما شامل علائم ریسک است.",
            ModerationDecision.Hold => "پیام برای بررسی انسانی نگه داشته شد.",
            ModerationDecision.Block => "پیام مسدود شد و به کاربر هشدار داده می‌شود.",
            ModerationDecision.BlockAndReport => "پیام مسدود و برای پیگیری ادمین پرچم شد.",
            _ => ""
        };

        UpdateTrust(candidate.UserId, decision);
        penalty = AdjustPenalty(penalty, decision);

        return new ModerationResult(Math.Round(risk, 2), matchedTags.ToList(), decision, notes, penalty);
    }

    private static GeminiPrompt BuildModerationPrompt(MessageCandidate candidate)
    {
        var instruction = new StringBuilder();
        instruction.AppendLine("You are an Iranian Farsi-speaking content safety reviewer.");
        instruction.AppendLine("Analyse the provided message according to the hackathon policy.");
        instruction.AppendLine("Return JSON with fields: riskScore (0-1), decision (Publish|SoftWarn|Hold|Block|BlockAndReport), policyTags (array of Farsi labels), notes (short Farsi explanation), penaltyPoints (integer).");
        instruction.AppendLine("Penalty guidance: Publish=0, SoftWarn=1, Hold=2-3, Block=3-5, BlockAndReport=5-10.");

        var userContent = $"پیام کاربر:\n{candidate.Content}\nنقش: {candidate.Channel}";
        return new GeminiPrompt(instruction.ToString(), userContent);
    }

    private static bool TryParseModeration(string body, out ModerationResult result)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<GeminiResponse>(body, GeminiJson.Options);
            var text = parsed?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                result = default!;
                return false;
            }

            var moderation = JsonSerializer.Deserialize<GeminiModerationPayload>(text, GeminiJson.Options);
            if (moderation is null)
            {
                result = default!;
                return false;
            }

            if (!Enum.TryParse<ModerationDecision>(moderation.Decision, true, out var decision))
            {
                decision = DetermineDecision(moderation.RiskScore);
            }

            result = new ModerationResult(
                Math.Clamp(moderation.RiskScore, 0, 1),
                moderation.PolicyTags ?? Array.Empty<string>(),
                decision,
                string.IsNullOrWhiteSpace(moderation.Notes) ? "بررسی با موفقیت انجام شد." : moderation.Notes!,
                AdjustPenalty(moderation.PenaltyPoints, decision));

            return true;
        }
        catch
        {
            result = default!;
            return false;
        }
    }

    private static ModerationDecision DetermineDecision(double risk)
        => risk switch
        {
            <= 0.2 => ModerationDecision.Publish,
            <= 0.4 => ModerationDecision.SoftWarn,
            <= 0.6 => ModerationDecision.Hold,
            <= 0.8 => ModerationDecision.Block,
            _ => ModerationDecision.BlockAndReport
        };

    private static void UpdateTrust(string userId, ModerationDecision decision)
    {
        if (decision is ModerationDecision.Publish or ModerationDecision.SoftWarn)
        {
            TrustAdjustments.AddOrUpdate(userId, _ => -0.05, (_, current) => Math.Max(-0.1, current - 0.05));
        }
        else
        {
            TrustAdjustments.AddOrUpdate(userId, _ => 0.1, (_, current) => Math.Min(0.2, current + 0.05));
        }
    }

    private static int AdjustPenalty(int penalty, ModerationDecision decision)
        => decision switch
        {
            ModerationDecision.Publish => 0,
            ModerationDecision.SoftWarn => Math.Max(penalty, 0),
            ModerationDecision.Hold => Math.Max(penalty, 1),
            ModerationDecision.Block => Math.Max(penalty, 3),
            ModerationDecision.BlockAndReport => Math.Max(penalty, 5),
            _ => penalty
        };

    private sealed record PolicyRule(string Keyword, string PolicyTag, double RiskScore, int PenaltyPoints);

    private sealed record GeminiGenerateRequest(string Model, string ApiKey, GeminiPrompt Prompt)
    {
        public HttpRequestMessage ToHttpRequest()
        {
            var payload = new
            {
                systemInstruction = new
                {
                    role = "system",
                    parts = new[] { new { text = Prompt.SystemInstruction } }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = Prompt.UserContent } }
                    }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    temperature = 0
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"models/{Model}:generateContent?key={ApiKey}")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, GeminiJson.Options), Encoding.UTF8, "application/json")
            };
            return request;
        }
    }

    private sealed record GeminiPrompt(string SystemInstruction, string UserContent);

    private sealed record GeminiResponse(IReadOnlyList<GeminiCandidate>? Candidates);
    private sealed record GeminiCandidate(GeminiContent? Content);
    private sealed record GeminiContent(IReadOnlyList<GeminiPart>? Parts);
    private sealed record GeminiPart(string? Text);

    private sealed record GeminiModerationPayload
    {
        public double RiskScore { get; init; }
        public string Decision { get; init; } = string.Empty;
        public string[]? PolicyTags { get; init; }
        public string? Notes { get; init; }
        public int PenaltyPoints { get; init; }
    }

    private static class GeminiJson
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };
    }
}
