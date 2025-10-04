using System.Net.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Common;
using NabTeams.Domain.Entities;
using NabTeams.Infrastructure.Services;
using Xunit;

namespace NabTeams.Api.Tests;

public class SupportResponderTests
{
    private static SupportResponder CreateResponder(params KnowledgeBaseItem[] items)
    {
        var knowledgeBase = new FakeKnowledgeBase(items);
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var options = new OptionsMonitorStub<GeminiOptions>(new GeminiOptions());
        return new SupportResponder(knowledgeBase, httpFactory.Object, options, NullLogger<SupportResponder>.Instance);
    }

    [Fact]
    public async Task ReturnsEscalationWhenNoMatches()
    {
        var responder = CreateResponder();

        var answer = await responder.GetAnswerAsync(new SupportQuery
        {
            Question = "چطور درخواست هزینه بدهم؟",
            Role = "participant"
        });

        Assert.True(answer.EscalateToHuman);
        Assert.Equal(0.2, answer.Confidence);
    }

    [Fact]
    public async Task PrefersAudienceSpecificEntries()
    {
        var responder = CreateResponder(
            new KnowledgeBaseItem
            {
                Id = "all-1",
                Title = "قانون عمومی",
                Body = "این قانون برای همه است.",
                Audience = "all",
                Tags = new[] { "rule" },
                UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new KnowledgeBaseItem
            {
                Id = "mentor-1",
                Title = "قانون منتورها",
                Body = "منتورها باید گزارش روزانه ثبت کنند.",
                Audience = "mentor",
                Tags = new[] { "mentor", "report" },
                UpdatedAt = DateTimeOffset.UtcNow
            });

        var answer = await responder.GetAnswerAsync(new SupportQuery
        {
            Question = "چطور گزارش ثبت کنم؟",
            Role = "mentor"
        });

        Assert.Contains("منتورها", answer.Answer);
        Assert.Contains("mentor-1", answer.Sources);
    }

    [Fact]
    public async Task FallsBackWhenGeminiFails()
    {
        var knowledgeBase = new FakeKnowledgeBase(new KnowledgeBaseItem
        {
            Id = "faq-1",
            Title = "زمان‌بندی",
            Body = "رویداد از ساعت 9 شروع می‌شود.",
            Audience = "participant",
            Tags = new[] { "schedule" },
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var httpFactory = new Mock<IHttpClientFactory>();
        var options = new OptionsMonitorStub<GeminiOptions>(new GeminiOptions { ApiKey = "test", Endpoint = "https://example" });
        var responder = new SupportResponder(knowledgeBase, httpFactory.Object, options, NullLogger<SupportResponder>.Instance);

        var answer = await responder.GetAnswerAsync(new SupportQuery
        {
            Question = "چه زمانی شروع می‌شود؟",
            Role = "participant"
        });

        Assert.False(answer.EscalateToHuman);
        Assert.Contains("رویداد", answer.Answer);
    }

    private sealed class FakeKnowledgeBase : ISupportKnowledgeBase
    {
        private readonly List<KnowledgeBaseItem> _items;

        public FakeKnowledgeBase(params KnowledgeBaseItem[] items)
        {
            _items = items.ToList();
        }

        public Task<IReadOnlyCollection<KnowledgeBaseItem>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<KnowledgeBaseItem>>(_items);

        public Task<KnowledgeBaseItem> UpsertAsync(KnowledgeBaseItem item, CancellationToken cancellationToken = default)
        {
            _items.Add(item);
            return Task.FromResult(item);
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            _items.RemoveAll(i => i.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class OptionsMonitorStub<T> : IOptionsMonitor<T>
    {
        public OptionsMonitorStub(T value)
        {
            CurrentValue = value;
        }

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
