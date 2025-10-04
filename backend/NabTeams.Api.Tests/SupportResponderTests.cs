using System.Net.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NabTeams.Api.Models;
using NabTeams.Api.Services;
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
                Id = "1",
                Title = "قانون عمومی",
                Audience = "all",
                Body = "تمام کاربران باید قوانین را رعایت کنند.",
                Tags = new[] { "قانون", "شرایط" },
                UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new KnowledgeBaseItem
            {
                Id = "2",
                Title = "راهنمای شرکت‌کننده",
                Audience = "participant",
                Body = "شرکت‌کنندگان باید پروژه را تا جمعه تحویل دهند.",
                Tags = new[] { "تحویل", "پروژه" },
                UpdatedAt = DateTimeOffset.UtcNow
            }
        );

        var answer = await responder.GetAnswerAsync(new SupportQuery
        {
            Question = "زمان تحویل پروژه شرکت‌کننده ها چه زمانی است؟",
            Role = "participant"
        });

        Assert.False(answer.EscalateToHuman);
        Assert.Contains("شرکت‌کنندگان", answer.Answer, StringComparison.Ordinal);
        Assert.Contains("2", answer.Sources);
        Assert.True(answer.Confidence > 0.3);
    }

    private sealed class FakeKnowledgeBase : ISupportKnowledgeBase
    {
        private readonly IReadOnlyCollection<KnowledgeBaseItem> _items;

        public FakeKnowledgeBase(IReadOnlyCollection<KnowledgeBaseItem> items)
        {
            _items = items;
        }

        public Task<IReadOnlyCollection<KnowledgeBaseItem>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_items);

        public Task<KnowledgeBaseItem> UpsertAsync(KnowledgeBaseItem item, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class OptionsMonitorStub<T> : IOptionsMonitor<T>
    {
        private readonly T _value;

        public OptionsMonitorStub(T value)
        {
            _value = value;
        }

        public T CurrentValue => _value;

        public T Get(string? name) => _value;

        public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose() { }
        }
    }
}
