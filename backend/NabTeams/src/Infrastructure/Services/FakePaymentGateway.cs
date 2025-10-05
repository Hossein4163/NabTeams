using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Infrastructure.Services;

public class FakePaymentGateway : IPaymentGateway
{
    private readonly PaymentGatewayOptions _options;

    public FakePaymentGateway(IOptions<PaymentGatewayOptions> options)
    {
        _options = options.Value;
    }

    public Task<RegistrationPayment> CreatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        var paymentId = Guid.NewGuid();
        var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? "https://payments.example.com/session"
            : _options.BaseUrl.TrimEnd('/');
        var paymentUrl = $"{baseUrl}/{paymentId}";

        var payment = new RegistrationPayment
        {
            Id = paymentId,
            ParticipantRegistrationId = request.ParticipantRegistrationId,
            Amount = request.Amount,
            Currency = request.Currency,
            PaymentUrl = paymentUrl,
            Status = RegistrationPaymentStatus.Pending,
            RequestedAt = DateTimeOffset.UtcNow
        };

        return Task.FromResult(payment);
    }
}
