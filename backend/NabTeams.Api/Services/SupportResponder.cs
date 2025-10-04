using System.Text;
using System.Text.RegularExpressions;
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

    public SupportResponder(ISupportKnowledgeBase knowledgeBase)
    {
        _knowledgeBase = knowledgeBase;
    }

    public async Task<SupportAnswer> GetAnswerAsync(SupportQuery query, CancellationToken cancellationToken = default)
    {
        var normalizedRole = string.IsNullOrWhiteSpace(query.Role) ? "all" : query.Role.Trim().ToLowerInvariant();
        var roleTokens = Tokenize(query.Question);

        if (roleTokens.Count == 0)
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

        var scored = new List<(KnowledgeBaseItem Item, double Score, double AudienceBoost)>();
        foreach (var item in candidates)
        {
            var itemTokens = Tokenize(string.Join(' ', item.Tags) + " " + item.Title + " " + item.Body);
            var overlap = roleTokens.Intersect(itemTokens).Count();
            var audienceBoost = item.Audience.Equals(normalizedRole, StringComparison.OrdinalIgnoreCase) ? 0.15 : 0;
            var tagBoost = item.Tags.Intersect(roleTokens, StringComparer.OrdinalIgnoreCase).Count() * 0.1;
            var score = (overlap / Math.Max(roleTokens.Count, 1)) + audienceBoost + tagBoost;
            if (score > 0)
            {
                scored.Add((item, score, audienceBoost + tagBoost));
            }
        }

        if (scored.Count == 0)
        {
            return new SupportAnswer
            {
                Answer = "برای این سوال پاسخ دقیقی ندارم. لطفاً سوال را دقیق‌تر بیان کنید یا با تیم پشتیبانی تماس بگیرید.",
                Confidence = 0.2,
                EscalateToHuman = true
            };
        }

        var ordered = scored
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.AudienceBoost)
            .Take(3)
            .ToList();

        var top = ordered.First();
        var supporting = ordered.Skip(1).Select(x => x.Item).ToList();

        var builder = new StringBuilder();
        builder.AppendLine(top.Item.Title + ":");
        builder.AppendLine(top.Item.Body.Trim());

        if (supporting.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("منابع مرتبط دیگر:");
            foreach (var item in supporting)
            {
                builder.AppendLine($"• {item.Title}: {item.Body}");
            }
        }

        var confidence = Math.Clamp(top.Score, 0, 1);

        return new SupportAnswer
        {
            Answer = builder.ToString().Trim(),
            Sources = ordered.Select(x => x.Item.Id).ToList(),
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
}
