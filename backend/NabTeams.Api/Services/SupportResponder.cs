using System.Text.RegularExpressions;
using NabTeams.Api.Models;

namespace NabTeams.Api.Services;

public interface ISupportKnowledgeBase
{
    IReadOnlyCollection<KnowledgeBaseItem> Items { get; }
}

public interface ISupportResponder
{
    Task<SupportAnswer> GetAnswerAsync(SupportQuery query, CancellationToken cancellationToken = default);
}

public class InMemorySupportKnowledgeBase : ISupportKnowledgeBase
{
    public IReadOnlyCollection<KnowledgeBaseItem> Items { get; } = new List<KnowledgeBaseItem>
    {
        new()
        {
            Id = "event-rules",
            Title = "قوانین کلی رویداد",
            Body = "شرکت‌کنندگان باید قوانین اخلاقی و حرفه‌ای را رعایت کنند. ساعات برگزاری از 9 تا 18 می‌باشد.",
            Audience = "participant",
            Tags = new[] { "rules", "schedule" }
        },
        new()
        {
            Id = "mentor-support",
            Title = "نقش منتورها",
            Body = "منتورها می‌توانند از طریق داشبورد منتور مستقیماً با تیم‌ها گفتگو کنند و دسترسی به اتاق‌های منتورینگ دارند.",
            Audience = "mentor",
            Tags = new[] { "mentor", "access" }
        },
        new()
        {
            Id = "contact-admin",
            Title = "راه‌های ارتباطی با ادمین",
            Body = "برای مسائل اضطراری با شماره 021-000000 تماس بگیرید یا از فرم تیکت در داشبورد استفاده کنید.",
            Audience = "all",
            Tags = new[] { "contact", "support" }
        },
        new()
        {
            Id = "investor-brief",
            Title = "دسترسی سرمایه‌گذاران",
            Body = "سرمایه‌گذاران به داشبورد ارزیابی مالی و گزارش‌های تیم‌ها دسترسی دارند. نسخه به‌روزشده هر روز ساعت 12 منتشر می‌شود.",
            Audience = "investor",
            Tags = new[] { "investor", "reports" }
        }
    };
}

public class SupportResponder : ISupportResponder
{
    private readonly ISupportKnowledgeBase _knowledgeBase;

    public SupportResponder(ISupportKnowledgeBase knowledgeBase)
    {
        _knowledgeBase = knowledgeBase;
    }

    public Task<SupportAnswer> GetAnswerAsync(SupportQuery query, CancellationToken cancellationToken = default)
    {
        var normalizedRole = string.IsNullOrWhiteSpace(query.Role) ? "all" : query.Role.ToLowerInvariant();
        var roleTokens = Tokenize(query.Question);
        var items = _knowledgeBase.Items
            .Where(item => item.Audience == "all" || item.Audience.Equals(normalizedRole, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!items.Any())
        {
            return Task.FromResult(new SupportAnswer
            {
                Answer = "هیچ منبعی برای نقش شما ثبت نشده است. لطفاً با ادمین تماس بگیرید.",
                Confidence = 0,
                EscalateToHuman = true
            });
        }

        var scored = new List<(KnowledgeBaseItem Item, double Score)>();
        foreach (var item in items)
        {
            var itemTokens = Tokenize(item.Body + " " + item.Title);
            var overlap = roleTokens.Intersect(itemTokens).Count();
            var score = overlap / (double)(roleTokens.Count + 1);
            if (score > 0)
            {
                scored.Add((item, score));
            }
        }

        if (!scored.Any())
        {
            return Task.FromResult(new SupportAnswer
            {
                Answer = "برای این سوال پاسخ دقیقی ندارم. لطفاً سوال را دقیق‌تر بیان کنید یا با تیم پشتیبانی تماس بگیرید.",
                Confidence = 0.2,
                EscalateToHuman = true
            });
        }

        var best = scored.OrderByDescending(x => x.Score).First();
        var confidence = Math.Clamp(best.Score, 0, 1);

        var answer = new SupportAnswer
        {
            Answer = $"{best.Item.Title}: {best.Item.Body}",
            Sources = new[] { best.Item.Id },
            Confidence = confidence,
            EscalateToHuman = confidence < 0.35
        };

        return Task.FromResult(answer);
    }

    private static IReadOnlyCollection<string> Tokenize(string text)
    {
        text = text.ToLowerInvariant();
        text = Regex.Replace(text, "[^\\p{L}\\p{Nd} ]+", " ");
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
