using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Common;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Infrastructure.Services;

public class IdPayPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly PaymentGatewayOptions _fallbackOptions;
    private readonly IIntegrationSettingsService _integrationSettings;

    public IdPayPaymentGateway(
        HttpClient httpClient,
        IIntegrationSettingsService integrationSettings,
        IOptions<PaymentGatewayOptions> options)
    {
        _httpClient = httpClient;
        _integrationSettings = integrationSettings;
        _fallbackOptions = options.Value;
    }

    public async Task<RegistrationPayment> CreatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        var resolved = await _integrationSettings.GetPaymentGatewayAsync(cancellationToken);
        var options = resolved?.Options ?? _fallbackOptions;

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("Payment gateway API key is missing. Configure provider settings in the admin panel.");
        }

        if (!string.Equals(options.Provider, "idpay", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Active payment gateway provider '{options.Provider}' is not supported by IdPayPaymentGateway.");
        }

        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        }

        var callbackBase = string.IsNullOrWhiteSpace(options.CallbackBaseUrl)
            ? throw new InvalidOperationException("Callback base URL is not configured for the payment gateway.")
            : options.CallbackBaseUrl.TrimEnd('/');

        var callbackUrl = $"{callbackBase}/api/registrations/participants/{request.ParticipantRegistrationId}/payments/callback";
        var amount = Convert.ToInt64(Math.Round(request.Amount, MidpointRounding.AwayFromZero));

        var payload = new
        {
            order_id = request.ParticipantRegistrationId.ToString("N"),
            amount,
            callback = callbackUrl,
            desc = "هزینه ورود به مرحله دوم رویداد ناب تیمز",
            name = "NabTeams Participant"
        };

        var createPath = string.IsNullOrWhiteSpace(options.CreatePath)
            ? "/v1.1/payment"
            : options.CreatePath;

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, createPath)
        {
            Content = JsonContent.Create(payload)
        };

        httpRequest.Headers.Add("X-API-KEY", options.ApiKey);
        httpRequest.Headers.Add("X-SANDBOX", options.Sandbox ? "1" : "0");

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<IdPayCreateResponse>(cancellationToken: cancellationToken);
        if (body is null || string.IsNullOrWhiteSpace(body.Link))
        {
            throw new InvalidOperationException("Payment gateway response did not include a link.");
        }

        return new RegistrationPayment
        {
            Id = Guid.NewGuid(),
            ParticipantRegistrationId = request.ParticipantRegistrationId,
            Amount = request.Amount,
            Currency = request.Currency,
            PaymentUrl = body.Link,
            Status = RegistrationPaymentStatus.Pending,
            RequestedAt = DateTimeOffset.UtcNow
        };
    }

    private sealed record IdPayCreateResponse(string Id, string Link);
}
