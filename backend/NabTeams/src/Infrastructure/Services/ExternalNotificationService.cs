using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Common;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Infrastructure.Services;

public class ExternalNotificationService : INotificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IIntegrationSettingsService _integrationSettings;
    private readonly ILogger<ExternalNotificationService> _logger;

    public ExternalNotificationService(
        IHttpClientFactory httpClientFactory,
        IIntegrationSettingsService integrationSettings,
        IOptions<NotificationOptions> options,
        ILogger<ExternalNotificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _integrationSettings = integrationSettings;
        _ = options.Value;
        _logger = logger;
    }

    public async Task<RegistrationNotification> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        var options = await _integrationSettings.GetNotificationOptionsAsync(cancellationToken);

        switch (request.Channel)
        {
            case NotificationChannel.Email:
                await SendEmailAsync(request, options.Email, cancellationToken);
                break;
            case NotificationChannel.Sms:
                await SendSmsAsync(request, options.Sms, cancellationToken);
                break;
            default:
                throw new NotSupportedException($"Notification channel {request.Channel} is not supported.");
        }

        return new RegistrationNotification
        {
            Id = Guid.NewGuid(),
            ParticipantRegistrationId = request.ParticipantRegistrationId,
            Channel = request.Channel,
            Recipient = request.Recipient,
            Subject = request.Subject,
            Message = request.Message,
            SentAt = DateTimeOffset.UtcNow
        };
    }

    private async Task SendEmailAsync(NotificationRequest request, NotificationOptions.EmailOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Host) || string.IsNullOrWhiteSpace(options.SenderAddress))
        {
            throw new InvalidOperationException("Email notification settings are incomplete. Configure Infrastructure:Notification:Email.");
        }

        using var smtpClient = new SmtpClient(options.Host, options.Port)
        {
            EnableSsl = options.UseSsl,
            Credentials = new NetworkCredential(options.Username, options.Password)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(options.SenderAddress, options.SenderDisplayName),
            Subject = request.Subject,
            Body = request.Message,
            BodyEncoding = System.Text.Encoding.UTF8,
            SubjectEncoding = System.Text.Encoding.UTF8
        };
        message.To.Add(new MailAddress(request.Recipient));

        await smtpClient.SendMailAsync(message, cancellationToken);
    }

    private async Task SendSmsAsync(NotificationRequest request, NotificationOptions.SmsOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("SMS notification settings are incomplete. Configure Infrastructure:Notification:Sms.");
        }

        var client = _httpClientFactory.CreateClient("notifications.sms");
        if (!string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        }

        var path = options.Path.Replace("{apiKey}", options.ApiKey, StringComparison.OrdinalIgnoreCase);
        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("receptor", request.Recipient),
            new KeyValuePair<string, string>("sender", options.SenderNumber),
            new KeyValuePair<string, string>("message", request.Message)
        });

        var response = await client.PostAsync(path, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("SMS provider returned {Status} for participant {ParticipantId}: {Body}", response.StatusCode, request.ParticipantRegistrationId, body);
            throw new InvalidOperationException($"Failed to send SMS notification: {response.StatusCode}");
        }
    }
}
