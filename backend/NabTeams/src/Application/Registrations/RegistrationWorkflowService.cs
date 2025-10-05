using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Application.Registrations;

public class RegistrationWorkflowService : IRegistrationWorkflowService
{
    private readonly IRegistrationRepository _repository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly INotificationService _notificationService;
    private readonly IBusinessPlanAnalyzer _businessPlanAnalyzer;

    public RegistrationWorkflowService(
        IRegistrationRepository repository,
        IPaymentGateway paymentGateway,
        INotificationService notificationService,
        IBusinessPlanAnalyzer businessPlanAnalyzer)
    {
        _repository = repository;
        _paymentGateway = paymentGateway;
        _notificationService = notificationService;
        _businessPlanAnalyzer = businessPlanAnalyzer;
    }

    public async Task<ParticipantRegistration?> ApproveParticipantAsync(
        Guid participantId,
        ParticipantApprovalOptions options,
        CancellationToken cancellationToken = default)
    {
        var registration = await _repository.GetParticipantAsync(participantId, cancellationToken);
        if (registration is null)
        {
            return null;
        }

        if (registration.Status == RegistrationStatus.PaymentCompleted || registration.Status == RegistrationStatus.Cancelled)
        {
            return registration;
        }

        await _repository.UpdateParticipantStatusAsync(participantId, RegistrationStatus.Approved, cancellationToken);

        var payment = registration.Payment;
        if (payment is null || payment.Status == RegistrationPaymentStatus.Failed || payment.Status == RegistrationPaymentStatus.Cancelled)
        {
            payment = await _paymentGateway.CreatePaymentAsync(
                new PaymentRequest(participantId, options.Amount, options.Currency, options.ReturnUrl),
                cancellationToken);
            payment = payment with { ParticipantRegistrationId = participantId };
            payment = await _repository.SaveParticipantPaymentAsync(participantId, payment, cancellationToken);
        }

        await _repository.UpdateParticipantStatusAsync(participantId, RegistrationStatus.PaymentRequested, cancellationToken);

        var notification = await _notificationService.SendAsync(
            new NotificationRequest(
                participantId,
                options.Recipient,
                "درخواست پرداخت مرحله دوم",
                $"تیم گرامی، درخواست شما تأیید شد. لطفاً هزینه ورود به مرحله بعد را از طریق لینک زیر پرداخت کنید:\n{payment.PaymentUrl}",
                NotificationChannel.Email),
            cancellationToken);

        await _repository.AddParticipantNotificationAsync(participantId, notification, cancellationToken);

        return await _repository.GetParticipantAsync(participantId, cancellationToken);
    }

    public async Task<ParticipantRegistration?> CompleteParticipantPaymentAsync(
        Guid participantId,
        string? gatewayReference,
        CancellationToken cancellationToken = default)
    {
        var registration = await _repository.GetParticipantAsync(participantId, cancellationToken);
        if (registration is null)
        {
            return null;
        }

        if (registration.Payment is null)
        {
            return registration;
        }

        if (registration.Payment.Status == RegistrationPaymentStatus.Completed)
        {
            await _repository.UpdateParticipantStatusAsync(participantId, RegistrationStatus.PaymentCompleted, cancellationToken);
            return await _repository.GetParticipantAsync(participantId, cancellationToken);
        }

        await _repository.UpdateParticipantPaymentStatusAsync(
            participantId,
            RegistrationPaymentStatus.Completed,
            gatewayReference,
            cancellationToken);

        await _repository.UpdateParticipantStatusAsync(participantId, RegistrationStatus.PaymentCompleted, cancellationToken);

        var notification = await _notificationService.SendAsync(
            new NotificationRequest(
                participantId,
                registration.Email ?? registration.PhoneNumber,
                "تأیید پرداخت ثبت‌نام",
                "پرداخت شما با موفقیت ثبت شد. منتظر اطلاعات تکمیلی منتورها باشید.",
                string.IsNullOrWhiteSpace(registration.Email) ? NotificationChannel.Sms : NotificationChannel.Email),
            cancellationToken);

        await _repository.AddParticipantNotificationAsync(participantId, notification, cancellationToken);

        return await _repository.GetParticipantAsync(participantId, cancellationToken);
    }

    public async Task<BusinessPlanReview?> AnalyzeBusinessPlanAsync(
        Guid participantId,
        BusinessPlanAnalysisOptions options,
        CancellationToken cancellationToken = default)
    {
        var registration = await _repository.GetParticipantAsync(participantId, cancellationToken);
        if (registration is null)
        {
            return null;
        }

        var attachments = options.AttachmentUrls?.Count > 0
            ? options.AttachmentUrls
            : registration.Documents.Select(d => d.FileUrl).ToList();

        var request = new BusinessPlanAnalysisRequest(
            participantId,
            options.Narrative,
            attachments,
            options.AdditionalContext);

        var review = await _businessPlanAnalyzer.AnalyzeAsync(request, cancellationToken);
        review = review with
        {
            ParticipantRegistrationId = participantId,
            Status = BusinessPlanReviewStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return await _repository.AddBusinessPlanReviewAsync(participantId, review, cancellationToken);
    }
}
