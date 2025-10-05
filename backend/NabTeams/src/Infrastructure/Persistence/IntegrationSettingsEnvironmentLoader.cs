using System;
using System.Collections.Generic;
using System.Text.Json;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Infrastructure.Persistence;

internal static class IntegrationSettingsEnvironmentLoader
{
    private static readonly IReadOnlyDictionary<string, (IntegrationProviderType Type, string DefaultProviderKey, string DefaultDisplayName)> KnownVariables =
        new Dictionary<string, (IntegrationProviderType, string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["INTEGRATIONS__GEMINI"] = (IntegrationProviderType.Gemini, "gemini", "Google Gemini"),
            ["INTEGRATIONS__PAYMENT_IDPAY"] = (IntegrationProviderType.PaymentGateway, "idpay", "IdPay"),
            ["INTEGRATIONS__EMAIL_SMTP"] = (IntegrationProviderType.Email, "smtp", "SMTP"),
            ["INTEGRATIONS__SMS_KAVENEGAR"] = (IntegrationProviderType.Sms, "kavenegar", "Kavenegar"),
        };

    public static IReadOnlyCollection<IntegrationSettingEntity> Load(DateTimeOffset timestamp)
    {
        var results = new List<IntegrationSettingEntity>();

        foreach (var pair in KnownVariables)
        {
            var raw = Environment.GetEnvironmentVariable(pair.Key);
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(raw);
                var root = document.RootElement;

                var providerKey = root.TryGetProperty("providerKey", out var providerKeyElement)
                    ? providerKeyElement.GetString()
                    : pair.Value.DefaultProviderKey;

                var displayName = root.TryGetProperty("displayName", out var displayNameElement)
                    ? displayNameElement.GetString()
                    : pair.Value.DefaultDisplayName;

                var isActive = root.TryGetProperty("isActive", out var activeElement)
                    ? activeElement.GetBoolean()
                    : true;

                var configuration = root.TryGetProperty("configuration", out var configElement)
                    ? configElement.GetRawText()
                    : root.GetRawText();

                results.Add(new IntegrationSettingEntity
                {
                    Id = Guid.NewGuid(),
                    Type = pair.Value.Type,
                    ProviderKey = providerKey ?? pair.Value.DefaultProviderKey,
                    DisplayName = displayName ?? pair.Value.DefaultDisplayName,
                    Configuration = configuration,
                    IsActive = isActive,
                    CreatedAt = timestamp,
                    UpdatedAt = timestamp,
                });
            }
            catch (JsonException)
            {
                // Ignore invalid JSON payloads so application startup is not blocked.
            }
        }

        return results;
    }
}
