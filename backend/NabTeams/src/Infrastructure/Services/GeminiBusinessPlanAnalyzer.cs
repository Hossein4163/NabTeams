using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Common;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Infrastructure.Services;

public class GeminiBusinessPlanAnalyzer : IBusinessPlanAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _fallbackOptions;
    private readonly IIntegrationSettingsService _integrationSettings;
    private readonly ILogger<GeminiBusinessPlanAnalyzer> _logger;

    public GeminiBusinessPlanAnalyzer(
        HttpClient httpClient,
        IIntegrationSettingsService integrationSettings,
        IOptions<GeminiOptions> options,
        ILogger<GeminiBusinessPlanAnalyzer> logger)
    {
        _httpClient = httpClient;
        _integrationSettings = integrationSettings;
        _fallbackOptions = options.Value;
        _logger = logger;
    }

    public async Task<BusinessPlanReview> AnalyzeAsync(BusinessPlanAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        var options = await _integrationSettings.GetGeminiOptionsAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured. Set Infrastructure:Gemini:ApiKey in configuration.");
        }

        var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
            ? _fallbackOptions.BaseUrl
            : options.BaseUrl;

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        }

        var model = string.IsNullOrWhiteSpace(options.BusinessPlanModel)
            ? options.RagModel
            : options.BusinessPlanModel;

        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("You are an accelerator mentor evaluating Iranian startup applications.");
        promptBuilder.AppendLine("Read the following business plan narrative and provide structured JSON feedback in Persian.");
        promptBuilder.AppendLine("Return a JSON object with keys: summary, strengths, risks, recommendations, score (0-100).");
        promptBuilder.AppendLine("Narrative:");
        promptBuilder.AppendLine(request.Narrative);

        if (request.AttachmentUrls.Any())
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Attachments the applicant has submitted (describe how you used them if relevant):");
            foreach (var url in request.AttachmentUrls)
            {
                promptBuilder.AppendLine(url);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.AdditionalContext))
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Additional context for evaluation:");
            promptBuilder.AppendLine(request.AdditionalContext);
        }

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = promptBuilder.ToString() }
                    }
                }
            },
            generationConfig = new
            {
                temperature = options.BusinessPlanTemperature,
                responseMimeType = "application/json"
            }
        };

        var endpoint = $"v1beta/models/{model}:generateContent?key={options.ApiKey}";
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(payload)
            };

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();
            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);
            if (geminiResponse?.Candidates is null || geminiResponse.Candidates.Length == 0)
            {
                throw new InvalidOperationException("Gemini response did not contain any candidates.");
            }

            var rawText = geminiResponse.Candidates[0].Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                throw new InvalidOperationException("Gemini response did not contain text content.");
            }

            var document = JsonDocument.Parse(rawText);
            var root = document.RootElement;

            var review = new BusinessPlanReview
            {
                Id = Guid.NewGuid(),
                ParticipantRegistrationId = request.ParticipantRegistrationId,
                Status = BusinessPlanReviewStatus.Completed,
                OverallScore = root.TryGetProperty("score", out var scoreElement) && scoreElement.TryGetDecimal(out var score)
                    ? score
                    : null,
                Summary = root.TryGetProperty("summary", out var summaryElement)
                    ? summaryElement.GetString() ?? string.Empty
                    : string.Empty,
                Strengths = root.TryGetProperty("strengths", out var strengthsElement)
                    ? strengthsElement.GetString() ?? string.Empty
                    : string.Empty,
                Risks = root.TryGetProperty("risks", out var risksElement)
                    ? risksElement.GetString() ?? string.Empty
                    : string.Empty,
                Recommendations = root.TryGetProperty("recommendations", out var recommendationsElement)
                    ? recommendationsElement.GetString() ?? string.Empty
                    : string.Empty,
                RawResponse = rawText,
                Model = model,
                SourceDocumentUrl = request.AttachmentUrls.FirstOrDefault(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            return review;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze business plan for participant {ParticipantId}", request.ParticipantRegistrationId);
            return new BusinessPlanReview
            {
                Id = Guid.NewGuid(),
                ParticipantRegistrationId = request.ParticipantRegistrationId,
                Status = BusinessPlanReviewStatus.Failed,
                Summary = "هوش مصنوعی نتوانست طرح را تحلیل کند. لطفاً بعداً مجدداً تلاش کنید.",
                Strengths = string.Empty,
                Risks = string.Empty,
                Recommendations = string.Empty,
                RawResponse = ex.Message,
                Model = model,
                SourceDocumentUrl = request.AttachmentUrls.FirstOrDefault(),
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
    }

    private sealed record GeminiResponse(GeminiCandidate[]? Candidates);

    private sealed record GeminiCandidate(GeminiContent? Content);

    private sealed record GeminiContent(GeminiPart[]? Parts);

    private sealed record GeminiPart(string? Text);
}
