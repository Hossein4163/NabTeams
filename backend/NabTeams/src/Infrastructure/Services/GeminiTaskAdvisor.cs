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
using NabTeams.Application.Tasks.Models;

namespace NabTeams.Infrastructure.Services;

public class GeminiTaskAdvisor : IAiTaskAdvisor
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _fallbackOptions;
    private readonly IIntegrationSettingsService _integrationSettingsService;
    private readonly ILogger<GeminiTaskAdvisor> _logger;

    public GeminiTaskAdvisor(
        HttpClient httpClient,
        IIntegrationSettingsService integrationSettingsService,
        IOptions<GeminiOptions> options,
        ILogger<GeminiTaskAdvisor> logger)
    {
        _httpClient = httpClient;
        _integrationSettingsService = integrationSettingsService;
        _fallbackOptions = options.Value;
        _logger = logger;
    }

    public async Task<TaskAdviceResult> GenerateAdviceAsync(TaskAdviceRequest request, CancellationToken cancellationToken = default)
    {
        var options = await _integrationSettingsService.GetGeminiOptionsAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured for task advising.");
        }

        var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
            ? _fallbackOptions.BaseUrl
            : options.BaseUrl;

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        }

        var model = string.IsNullOrWhiteSpace(options.RagModel)
            ? "gemini-1.5-flash-latest"
            : options.RagModel;

        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("You are an AI project manager helping Iranian startup teams organise their work.");
        promptBuilder.AppendLine("Review the context and suggest actionable tasks in Persian using bullet lists.");
        promptBuilder.AppendLine("Return JSON with keys: summary (string), suggestedTasks (array of strings), risks (string), nextSteps (string).");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Context:");
        promptBuilder.AppendLine(request.TaskContext);

        if (request.ExistingTasks.Any())
        {
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Existing tasks and statuses:");
            foreach (var task in request.ExistingTasks)
            {
                promptBuilder.AppendLine(task);
            }
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
            var rawText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(rawText))
            {
                throw new InvalidOperationException("Gemini response was empty.");
            }

            var document = JsonDocument.Parse(rawText);
            var root = document.RootElement;

            return new TaskAdviceResult
            {
                Summary = root.TryGetProperty("summary", out var summaryElement)
                    ? summaryElement.GetString() ?? string.Empty
                    : string.Empty,
                SuggestedTasks = root.TryGetProperty("suggestedTasks", out var tasksElement) && tasksElement.ValueKind == JsonValueKind.Array
                    ? tasksElement.EnumerateArray().Select(x => x.GetString() ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray()
                    : Array.Empty<string>(),
                Risks = root.TryGetProperty("risks", out var risksElement)
                    ? risksElement.GetString()
                    : null,
                NextSteps = root.TryGetProperty("nextSteps", out var stepsElement)
                    ? stepsElement.GetString()
                    : null,
                RawResponse = rawText
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI task advice for participant {Participant}", request.ParticipantRegistrationId);
            return new TaskAdviceResult
            {
                Summary = "هوش مصنوعی در حال حاضر در دسترس نیست. لطفاً بعداً تلاش کنید.",
                SuggestedTasks = Array.Empty<string>(),
                Risks = null,
                NextSteps = null,
                RawResponse = ex.Message
            };
        }
    }

    private sealed record GeminiResponse(GeminiCandidate[]? Candidates);
    private sealed record GeminiCandidate(GeminiContent? Content);
    private sealed record GeminiContent(GeminiPart[]? Parts);
    private sealed record GeminiPart(string? Text);
}
