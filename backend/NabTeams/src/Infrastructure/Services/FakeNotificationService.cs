using System;
using System.Threading;
using System.Threading.Tasks;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Infrastructure.Services;

public class FakeNotificationService : INotificationService
{
    public Task<RegistrationNotification> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        var log = new RegistrationNotification
        {
            Id = Guid.NewGuid(),
            ParticipantRegistrationId = request.ParticipantRegistrationId,
            Channel = request.Channel,
            Recipient = request.Recipient,
            Subject = request.Subject,
            Message = request.Message,
            SentAt = DateTimeOffset.UtcNow
        };

        return Task.FromResult(log);
    }
}
