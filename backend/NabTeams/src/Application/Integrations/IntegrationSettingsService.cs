using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Common;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Application.Integrations;

public class IntegrationSettingsService : IIntegrationSettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IIntegrationSettingsRepository _repository;
    private readonly IOptions<GeminiOptions> _geminiDefaults;
    private readonly IOptions<PaymentGatewayOptions> _paymentDefaults;
    private readonly IOptions<NotificationOptions> _notificationDefaults;
    private readonly ILogger<IntegrationSettingsService> _logger;

    public IntegrationSettingsService(
        IIntegrationSettingsRepository repository,
        IOptions<GeminiOptions> geminiDefaults,
        IOptions<PaymentGatewayOptions> paymentDefaults,
        IOptions<NotificationOptions> notificationDefaults,
        ILogger<IntegrationSettingsService> logger)
    {
        _repository = repository;
        _geminiDefaults = geminiDefaults;
        _paymentDefaults = paymentDefaults;
        _notificationDefaults = notificationDefaults;
        _logger = logger;
    }

    public Task<IReadOnlyCollection<IntegrationSetting>> ListAsync(
        IntegrationProviderType? type,
        CancellationToken cancellationToken = default)
        => _repository.ListAsync(type, cancellationToken);

    public Task<IntegrationSetting?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetAsync(id, cancellationToken);

    public async Task<IntegrationSetting> UpsertAsync(
        IntegrationSetting setting,
        bool activate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(setting.ProviderKey))
        {
            throw new ArgumentException("ProviderKey cannot be empty", nameof(setting));
        }

        var normalizedProvider = setting.ProviderKey.Trim();
        var normalizedDisplay = string.IsNullOrWhiteSpace(setting.DisplayName)
            ? normalizedProvider
            : setting.DisplayName.Trim();

        var payload = setting with
        {
            ProviderKey = normalizedProvider,
            DisplayName = normalizedDisplay,
            Configuration = setting.Configuration ?? string.Empty,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedAt = setting.Id == Guid.Empty ? DateTimeOffset.UtcNow : setting.CreatedAt,
            Id = setting.Id == Guid.Empty ? Guid.NewGuid() : setting.Id
        };

        var saved = await _repository.UpsertAsync(payload, cancellationToken);
        if (activate)
        {
            await _repository.SetActiveAsync(saved.Id, cancellationToken);
            saved = saved with { IsActive = true };
        }

        return saved;
    }

    public Task SetActiveAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.SetActiveAsync(id, cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);

    public async Task<GeminiOptions> GetGeminiOptionsAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _repository.GetActiveAsync(IntegrationProviderType.Gemini, cancellationToken);
        if (setting is null || string.IsNullOrWhiteSpace(setting.Configuration))
        {
            return _geminiDefaults.Value;
        }

        try
        {
            var options = JsonSerializer.Deserialize<GeminiOptions>(setting.Configuration, SerializerOptions);
            return options ?? _geminiDefaults.Value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Gemini integration settings");
            return _geminiDefaults.Value;
        }
    }

    public async Task<(IntegrationSetting Setting, PaymentGatewayOptions Options)?> GetPaymentGatewayAsync(
        CancellationToken cancellationToken = default)
    {
        var setting = await _repository.GetActiveAsync(IntegrationProviderType.PaymentGateway, cancellationToken);
        if (setting is null || string.IsNullOrWhiteSpace(setting.Configuration))
        {
            return null;
        }

        try
        {
            var options = JsonSerializer.Deserialize<PaymentGatewayOptions>(setting.Configuration, SerializerOptions)
                ?? _paymentDefaults.Value;
            options.Provider = setting.ProviderKey;
            return (setting, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse payment gateway configuration for provider {Provider}", setting.ProviderKey);
            return null;
        }
    }

    public async Task<NotificationOptions> GetNotificationOptionsAsync(CancellationToken cancellationToken = default)
    {
        var emailSetting = await _repository.GetActiveAsync(IntegrationProviderType.Email, cancellationToken);
        var smsSetting = await _repository.GetActiveAsync(IntegrationProviderType.Sms, cancellationToken);

        var defaults = _notificationDefaults.Value;
        var result = new NotificationOptions
        {
            Email = new NotificationOptions.EmailOptions
            {
                Provider = defaults.Email.Provider,
                Host = defaults.Email.Host,
                Port = defaults.Email.Port,
                UseSsl = defaults.Email.UseSsl,
                Username = defaults.Email.Username,
                Password = defaults.Email.Password,
                SenderAddress = defaults.Email.SenderAddress,
                SenderDisplayName = defaults.Email.SenderDisplayName
            },
            Sms = new NotificationOptions.SmsOptions
            {
                Provider = defaults.Sms.Provider,
                BaseUrl = defaults.Sms.BaseUrl,
                ApiKey = defaults.Sms.ApiKey,
                SenderNumber = defaults.Sms.SenderNumber,
                Path = defaults.Sms.Path
            }
        };

        if (emailSetting is not null && !string.IsNullOrWhiteSpace(emailSetting.Configuration))
        {
            try
            {
                var email = JsonSerializer.Deserialize<NotificationOptions.EmailOptions>(emailSetting.Configuration, SerializerOptions);
                if (email is not null)
                {
                    email.Provider = emailSetting.ProviderKey;
                    result.Email = email;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse email notification settings for provider {Provider}", emailSetting.ProviderKey);
            }
        }

        if (smsSetting is not null && !string.IsNullOrWhiteSpace(smsSetting.Configuration))
        {
            try
            {
                var sms = JsonSerializer.Deserialize<NotificationOptions.SmsOptions>(smsSetting.Configuration, SerializerOptions);
                if (sms is not null)
                {
                    sms.Provider = smsSetting.ProviderKey;
                    result.Sms = sms;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse SMS notification settings for provider {Provider}", smsSetting.ProviderKey);
            }
        }

        return result;
    }
}
