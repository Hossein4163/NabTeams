using System;
using System.Threading;
using System.Threading.Tasks;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Application.Abstractions;

public record NotificationRequest(
    Guid ParticipantRegistrationId,
    string Recipient,
    string Subject,
    string Message,
    NotificationChannel Channel);

public interface INotificationService
{
    Task<RegistrationNotification> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default);
}
