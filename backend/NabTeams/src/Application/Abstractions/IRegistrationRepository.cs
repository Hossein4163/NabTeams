using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Application.Abstractions;

public interface IRegistrationRepository
{
    Task<ParticipantRegistration> AddParticipantAsync(ParticipantRegistration registration, CancellationToken cancellationToken = default);
    Task<ParticipantRegistration?> GetParticipantAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ParticipantRegistration>> ListParticipantsAsync(CancellationToken cancellationToken = default);
    Task<ParticipantRegistration?> UpdateParticipantAsync(Guid id, ParticipantRegistration registration, CancellationToken cancellationToken = default);
    Task<ParticipantRegistration?> FinalizeParticipantAsync(Guid id, string? summaryFileUrl, CancellationToken cancellationToken = default);
    Task<ParticipantRegistration?> UpdateParticipantStatusAsync(Guid id, RegistrationStatus status, CancellationToken cancellationToken = default);
    Task<RegistrationPayment> SaveParticipantPaymentAsync(Guid participantId, RegistrationPayment payment, CancellationToken cancellationToken = default);
    Task<RegistrationPayment?> UpdateParticipantPaymentStatusAsync(Guid participantId, RegistrationPaymentStatus status, string? gatewayReference, CancellationToken cancellationToken = default);
    Task<RegistrationNotification> AddParticipantNotificationAsync(Guid participantId, RegistrationNotification notification, CancellationToken cancellationToken = default);
    Task<BusinessPlanReview> AddBusinessPlanReviewAsync(Guid participantId, BusinessPlanReview review, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<BusinessPlanReview>> ListBusinessPlanReviewsAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<BusinessPlanReview?> GetBusinessPlanReviewAsync(Guid participantId, Guid reviewId, CancellationToken cancellationToken = default);

    Task<JudgeRegistration> AddJudgeAsync(JudgeRegistration registration, CancellationToken cancellationToken = default);
    Task<JudgeRegistration?> GetJudgeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<JudgeRegistration>> ListJudgesAsync(CancellationToken cancellationToken = default);

    Task<InvestorRegistration> AddInvestorAsync(InvestorRegistration registration, CancellationToken cancellationToken = default);
    Task<InvestorRegistration?> GetInvestorAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<InvestorRegistration>> ListInvestorsAsync(CancellationToken cancellationToken = default);
}
