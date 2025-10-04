using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using NabTeams.Api.Models;

namespace NabTeams.Api.Services;

public interface ISupportKnowledgeBase
{
    Task<IReadOnlyCollection<KnowledgeBaseItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<KnowledgeBaseItem> UpsertAsync(KnowledgeBaseItem item, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public interface ISupportResponder
{
    Task<SupportAnswer> GetAnswerAsync(SupportQuery query, CancellationToken cancellationToken = default);
}

public class SupportResponder : ISupportResponder
{
    private readonly ISupportKnowledgeBase _knowledgeBase;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<GeminiOptions> _options;
    private readonly ILogger<SupportResponder> _logger;

    public SupportResponder(
        ISupportKnowledgeBase knowledgeBase,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<GeminiOptions> options,
        ILogger<SupportResponder> logger)
    {
        _knowledgeBase = knowledgeBase;
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<SupportAnswer> GetAnswerAsync(SupportQuery query, CancellationToken cancellationToken = default)
    {
        var normalizedRole = string.IsNullOrWhiteSpace(query.Role) ? "all" : query.Role.Trim().ToLowerInvariant();
        var questionTokens = Tokenize(query.Question);

        if (questionTokens.Count == 0)
        {
            return new SupportAnswer
            {
                Answer = "لطفاً سوال را با جزئیات بیشتری مطرح کنید تا بتوانم کمک کنم.",
                Confidence = 0,
                EscalateToHuman = false
            };
        }

        var items = await _knowledgeBase.GetAllAsync(cancellationToken);
        var candidates = items
            .Where(item => item.Audience.Equals("all", StringComparison.OrdinalIgnoreCase) || item.Audience.Equals(normalizedRole, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0)
        {
            return new SupportAnswer
            {
                Answer = "هیچ منبعی برای نقش شما ثبت نشده است. لطفاً با ادمین تماس بگیرید.",
                Confidence = 0,
                EscalateToHuman = true
            };
        }

        var scored = RankCandidates(questionTokens, candidates, normalizedRole);
        if (scored.Count == 0)
        {
            return new SupportAnswer
            {
                Answer = "برای این سوال پاسخ دقیقی ندارم. لطفاً سوال را دقیق‌تر بیان کنید یا با تیم پشتیبانی تماس بگیرید.",
                Confidence = 0.2,
                EscalateToHuman = true
            };
        }

        var options = _options.CurrentValue;
        if (options.Enabled)
        {
            try
            {
                return await AskGeminiAsync(query.Question, scored, options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini RAG request failed; falling back to heuristic response");
            }
        }

        return BuildHeuristicAnswer(scored);
    }

    private static List<(KnowledgeBaseItem Item, double Score)> RankCandidates(IReadOnlyCollection<string> questionTokens, IReadOnlyCollection<KnowledgeBaseItem> candidates, string normalizedRole)
    {
        var ranked = new List<(KnowledgeBaseItem Item, double Score)>();
        foreach (var item in candidates)
        {
            var itemTokens = Tokenize(string.Join(' ', item.Tags) + " " + item.Title + " " + item.Body);
            var overlap = questionTokens.Intersect(itemTokens).Count();
            var audienceBoost = item.Audience.Equals(normalizedRole, StringComparison.OrdinalIgnoreCase) ? 0.2 : 0;
            var tagBoost = item.Tags.Intersect(questionTokens, StringComparer.OrdinalIgnoreCase).Count() * 0.1;
            var score = (overlap / Math.Max(questionTokens.Count, 1)) + audienceBoost + tagBoost;
            if (score > 0)
            {
                ranked.Add((item, Math.Min(score, 1))); // clamp scores to keep ratios meaningful
            }
        }

        return ranked
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Item.UpdatedAt)
            .Take(5)
            .ToList();
    }

    private async Task<SupportAnswer> AskGeminiAsync(string question, IReadOnlyCollection<(KnowledgeBaseItem Item, double Score)> scored, GeminiOptions options, CancellationToken cancellationToken)
    {
        var contextBuilder = new StringBuilder();
        foreach (var (item, score) in scored)
        {
            contextBuilder.AppendLine($"شناسه: {item.Id}");
            contextBuilder.AppendLine($"عنوان: {item.Title}");
            contextBuilder.AppendLine($"متن: {item.Body}");
            contextBuilder.AppendLine($"نقش: {item.Audience}");
            contextBuilder.AppendLine($"امتیاز تقریبی: {score:0.00}");
            contextBuilder.AppendLine("---");
        }

        var client = _httpClientFactory.CreateClient("gemini");
        var instruction = new StringBuilder();
        instruction.AppendLine("You are a knowledgeable Persian support assistant for a hackathon management platform.");
        instruction.AppendLine("Answer in Persian (fa-IR) using only the provided context snippets.");
        instruction.AppendLine("Return JSON with fields: answer (string), sources (array of snippet ids), confidence (0-1), escalateToHuman (boolean).");
        instruction.AppendLine("If information is missing, set escalateToHuman to true and explain what is unclear.");

        var userContent = $"پرسش:\n{question}\n\nمنابع:\n{contextBuilder}";
        var payload = new
        {
            systemInstruction = new
            {
                role = "system",
                parts = new[] { new { text = instruction.ToString() } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = userContent } }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                temperature = 0.2
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"models/{options.RagModel}:generateContent?key={options.ApiKey}")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, GeminiJson.Options), Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        var parsed = JsonSerializer.Deserialize<GeminiResponse>(body, GeminiJson.Options);
        var text = parsed?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Gemini پاسخ خالی برگرداند.");
        }

        var ragPayload = JsonSerializer.Deserialize<GeminiSupportPayload>(text, GeminiJson.Options);
        if (ragPayload is null)
        {
            throw new InvalidOperationException("ساختار پاسخ Gemini قابل پردازش نبود.");
        }

        return new SupportAnswer
        {
            Answer = ragPayload.Answer ?? "پاسخی یافت نشد.",
            Sources = ragPayload.Sources ?? scored.Select(x => x.Item.Id).ToList(),
            Confidence = Math.Clamp(ragPayload.Confidence, 0, 1),
            EscalateToHuman = ragPayload.EscalateToHuman
        };
    }

    private static SupportAnswer BuildHeuristicAnswer(IReadOnlyCollection<(KnowledgeBaseItem Item, double Score)> scored)
    {
        var primary = scored.First();
        var supporting = scored.Skip(1).ToList();
        var builder = new StringBuilder();
        builder.AppendLine(primary.Item.Title + ":");
        builder.AppendLine(primary.Item.Body.Trim());

        if (supporting.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("منابع مرتبط دیگر:");
            foreach (var item in supporting)
            {
                builder.AppendLine($"• {item.Item.Title}: {item.Item.Body}");
            }
        }

        var confidence = Math.Clamp(primary.Score, 0, 1);
        return new SupportAnswer
        {
            Answer = builder.ToString().Trim(),
            Sources = scored.Select(x => x.Item.Id).ToList(),
            Confidence = confidence,
            EscalateToHuman = confidence < 0.35
        };
    }

    private static IReadOnlyCollection<string> Tokenize(string text)
    {
        text = text.ToLowerInvariant();
        text = Regex.Replace(text, "[^\\p{L}\\p{Nd} ]+", " ");
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private sealed record GeminiResponse(IReadOnlyList<GeminiCandidate>? Candidates);
    private sealed record GeminiCandidate(GeminiContent? Content);
    private sealed record GeminiContent(IReadOnlyList<GeminiPart>? Parts);
    private sealed record GeminiPart(string? Text);

    private sealed record GeminiSupportPayload
    {
        public string? Answer { get; init; }
        public List<string>? Sources { get; init; }
        public double Confidence { get; init; }
        public bool EscalateToHuman { get; init; }
    }

    private static class GeminiJson
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };
    }
}
