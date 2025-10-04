using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Common;
using NabTeams.Domain.Entities;

namespace NabTeams.Infrastructure.Services;

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
                ranked.Add((item, Math.Min(score, 1)));
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
                temperature = 0.2,
                maxOutputTokens = 1024
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{options.Endpoint}/models/{options.RagModel}:generateContent")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-goog-api-key", options.ApiKey);

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var resultJson = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            var document = JsonDocument.Parse(resultJson);
            var text = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Gemini response missing text");
            }

            var normalizedJson = Regex.Replace(text, @"(?<=[}\]\"]),(?=\s*[}\]])", string.Empty);
            var answer = JsonSerializer.Deserialize<SupportAnswer>(normalizedJson);
            if (answer is null)
            {
                throw new InvalidOperationException("Gemini response could not be parsed into SupportAnswer");
            }

            if (answer.Sources.Count == 0)
            {
                answer = answer with { Sources = scored.Select(s => s.Item.Id).ToArray() };
            }

            return answer;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini returned unexpected payload; falling back to heuristic answer. Body: {Body}", resultJson);
            return BuildHeuristicAnswer(scored);
        }
    }

    private static SupportAnswer BuildHeuristicAnswer(IReadOnlyCollection<(KnowledgeBaseItem Item, double Score)> scored)
    {
        var top = scored.First();
        return new SupportAnswer
        {
            Answer = top.Item.Body,
            Sources = new[] { top.Item.Id },
            Confidence = top.Score,
            EscalateToHuman = top.Score < 0.4
        };
    }

    private static HashSet<string> Tokenize(string input)
    {
        var tokens = Regex.Split(input.ToLowerInvariant(), @"[^\p{L}\p{N}]+", RegexOptions.Compiled)
            .Where(token => token.Length > 1)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return tokens;
    }
}
