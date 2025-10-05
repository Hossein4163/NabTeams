using System;
using System.Threading;
using System.Threading.Tasks;
using NabTeams.Domain.Entities;

namespace NabTeams.Application.Abstractions;

public record ParticipantApprovalOptions(
    decimal Amount,
    string Currency,
    string Recipient,
    string ReturnUrl);

public interface IRegistrationWorkflowService
{
    Task<ParticipantRegistration?> ApproveParticipantAsync(Guid participantId, ParticipantApprovalOptions options, CancellationToken cancellationToken = default);
    Task<ParticipantRegistration?> CompleteParticipantPaymentAsync(Guid participantId, string? gatewayReference, CancellationToken cancellationToken = default);
}
