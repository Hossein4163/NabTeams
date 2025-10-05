using System;
using System.Threading;
using System.Threading.Tasks;
using NabTeams.Domain.Entities;

namespace NabTeams.Application.Abstractions;

public record PaymentRequest(
    Guid ParticipantRegistrationId,
    decimal Amount,
    string Currency,
    string ReturnUrl);

public interface IPaymentGateway
{
    Task<RegistrationPayment> CreatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);
}
