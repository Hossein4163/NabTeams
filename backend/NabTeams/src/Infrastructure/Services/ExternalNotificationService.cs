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
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Infrastructure.Services;

public class ExternalNotificationService : INotificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly NotificationOptions _options;
    private readonly ILogger<ExternalNotificationService> _logger;

    public ExternalNotificationService(
        IHttpClientFactory httpClientFactory,
        IOptions<NotificationOptions> options,
        ILogger<ExternalNotificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RegistrationNotification> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        switch (request.Channel)
        {
            case NotificationChannel.Email:
                await SendEmailAsync(request, cancellationToken);
                break;
            case NotificationChannel.Sms:
                await SendSmsAsync(request, cancellationToken);
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

    private async Task SendEmailAsync(NotificationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Email.Host) || string.IsNullOrWhiteSpace(_options.Email.SenderAddress))
        {
            throw new InvalidOperationException("Email notification settings are incomplete. Configure Infrastructure:Notification:Email.");
        }

        using var smtpClient = new SmtpClient(_options.Email.Host, _options.Email.Port)
        {
            EnableSsl = _options.Email.UseSsl,
            Credentials = new NetworkCredential(_options.Email.Username, _options.Email.Password)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_options.Email.SenderAddress, _options.Email.SenderDisplayName),
            Subject = request.Subject,
            Body = request.Message,
            BodyEncoding = System.Text.Encoding.UTF8,
            SubjectEncoding = System.Text.Encoding.UTF8
        };
        message.To.Add(new MailAddress(request.Recipient));

        await smtpClient.SendMailAsync(message, cancellationToken);
    }

    private async Task SendSmsAsync(NotificationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Sms.ApiKey))
        {
            throw new InvalidOperationException("SMS notification settings are incomplete. Configure Infrastructure:Notification:Sms.");
        }

        var client = _httpClientFactory.CreateClient("notifications.sms");
        if (client.BaseAddress is null)
        {
            client.BaseAddress = new Uri(_options.Sms.BaseUrl);
        }

        var path = _options.Sms.Path.Replace("{apiKey}", _options.Sms.ApiKey, StringComparison.OrdinalIgnoreCase);
        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("receptor", request.Recipient),
            new KeyValuePair<string, string>("sender", _options.Sms.SenderNumber),
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
