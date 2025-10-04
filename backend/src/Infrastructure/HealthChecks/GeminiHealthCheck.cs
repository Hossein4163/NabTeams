using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NabTeams.Application.Common;

namespace NabTeams.Infrastructure.HealthChecks;

public class GeminiHealthCheck : IHealthCheck
{
    private static readonly byte[] ProbePayload = Encoding.UTF8.GetBytes(
        "{\"contents\":[{\"role\":\"user\",\"parts\":[{\"text\":\"health probe\"}]}]}");

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<GeminiOptions> _options;
    private readonly ILogger<GeminiHealthCheck> _logger;

    public GeminiHealthCheck(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<GeminiOptions> options,
        ILogger<GeminiHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        if (!options.Enabled)
        {
            return HealthCheckResult.Healthy("Gemini integration disabled; rule-based moderation active.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return HealthCheckResult.Degraded("Gemini API key missing.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient("gemini");
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"models/{options.ModerationModel}:countTokens?key={options.ApiKey}");

            var content = new ByteArrayContent(ProbePayload);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            request.Content = content;

            var response = await client.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Gemini reachable.");
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                return HealthCheckResult.Unhealthy("Gemini authentication failed. Check API key configuration.");
            }

            return HealthCheckResult.Degraded($"Gemini responded with status {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini health probe failed");
            return HealthCheckResult.Degraded("Unable to reach Gemini moderation endpoint.", ex);
        }
    }
}
