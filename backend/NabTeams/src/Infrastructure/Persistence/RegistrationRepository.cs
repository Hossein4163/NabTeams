using Microsoft.EntityFrameworkCore;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NabTeams.Infrastructure.Persistence;

public class EfRegistrationRepository : IRegistrationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EfRegistrationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ParticipantRegistration> AddParticipantAsync(ParticipantRegistration registration, CancellationToken cancellationToken = default)
    {
        var entity = registration.ToEntity();
        _dbContext.ParticipantRegistrations.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    public async Task<ParticipantRegistration?> GetParticipantAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ParticipantRegistrations
            .AsNoTracking()
            .Include(x => x.Members)
            .Include(x => x.Documents)
            .Include(x => x.Links)
            .Include(x => x.Payment)
            .Include(x => x.Notifications)
            .Include(x => x.BusinessPlanReviews)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity?.ToModel();
    }

    public async Task<IReadOnlyCollection<ParticipantRegistration>> ListParticipantsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.ParticipantRegistrations
            .AsNoTracking()
            .Include(x => x.Members)
            .Include(x => x.Documents)
            .Include(x => x.Links)
            .Include(x => x.Payment)
            .Include(x => x.Notifications)
            .Include(x => x.BusinessPlanReviews)
            .OrderByDescending(x => x.FinalizedAt ?? x.SubmittedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return entities.Select(x => x.ToModel()).ToList();
    }

    public async Task<ParticipantRegistration?> UpdateParticipantAsync(
        Guid id,
        ParticipantRegistration registration,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ParticipantRegistrations
            .Include(x => x.Members)
            .Include(x => x.Documents)
            .Include(x => x.Links)
            .Include(x => x.Payment)
            .Include(x => x.Notifications)
            .Include(x => x.BusinessPlanReviews)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        if (entity.Status == RegistrationStatus.Finalized)
        {
            return entity.ToModel();
        }

        entity.HeadFirstName = registration.HeadFirstName;
        entity.HeadLastName = registration.HeadLastName;
        entity.NationalId = registration.NationalId;
        entity.PhoneNumber = registration.PhoneNumber;
        entity.Email = registration.Email;
        entity.BirthDate = registration.BirthDate;
        entity.EducationDegree = registration.EducationDegree;
        entity.FieldOfStudy = registration.FieldOfStudy;
        entity.TeamName = registration.TeamName;
        entity.HasTeam = registration.HasTeam;
        entity.TeamCompleted = registration.TeamCompleted;
        entity.AdditionalNotes = registration.AdditionalNotes;

        entity.UpdateCollections(registration);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    public async Task<ParticipantRegistration?> FinalizeParticipantAsync(
        Guid id,
        string? summaryFileUrl,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ParticipantRegistrations
            .Include(x => x.Members)
            .Include(x => x.Documents)
            .Include(x => x.Links)
            .Include(x => x.Payment)
            .Include(x => x.Notifications)
            .Include(x => x.BusinessPlanReviews)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        if (entity.Status == RegistrationStatus.Finalized)
        {
            return entity.ToModel();
        }

        entity.Status = RegistrationStatus.Finalized;
        entity.FinalizedAt = DateTimeOffset.UtcNow;
        entity.SummaryFileUrl = string.IsNullOrWhiteSpace(summaryFileUrl)
            ? entity.SummaryFileUrl
            : summaryFileUrl.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    public async Task<ParticipantRegistration?> UpdateParticipantStatusAsync(
        Guid id,
        RegistrationStatus status,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ParticipantRegistrations
            .Include(x => x.Members)
            .Include(x => x.Documents)
            .Include(x => x.Links)
            .Include(x => x.Payment)
            .Include(x => x.Notifications)
            .Include(x => x.BusinessPlanReviews)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Status = status;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    public async Task<RegistrationPayment> SaveParticipantPaymentAsync(
        Guid participantId,
        RegistrationPayment payment,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ParticipantRegistrations
            .Include(x => x.Payment)
            .SingleOrDefaultAsync(x => x.Id == participantId, cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException($"Participant registration {participantId} not found");
        }

        if (entity.Payment is null)
        {
            var paymentEntity = payment.ToEntity();
            paymentEntity.ParticipantRegistrationId = participantId;
            entity.Payment = paymentEntity;
            _dbContext.RegistrationPayments.Add(paymentEntity);
        }
        else
        {
            entity.Payment.Amount = payment.Amount;
            entity.Payment.Currency = payment.Currency;
            entity.Payment.PaymentUrl = payment.PaymentUrl;
            entity.Payment.Status = payment.Status;
            entity.Payment.RequestedAt = payment.RequestedAt;
            entity.Payment.CompletedAt = payment.CompletedAt;
            entity.Payment.GatewayReference = payment.GatewayReference;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (entity.Payment ?? throw new InvalidOperationException()).ToModel();
    }

    public async Task<RegistrationPayment?> UpdateParticipantPaymentStatusAsync(
        Guid participantId,
        RegistrationPaymentStatus status,
        string? gatewayReference,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ParticipantRegistrations
            .Include(x => x.Payment)
            .SingleOrDefaultAsync(x => x.Id == participantId, cancellationToken);

        if (entity?.Payment is null)
        {
            return null;
        }

        entity.Payment.Status = status;
        if (status == RegistrationPaymentStatus.Completed)
        {
            entity.Payment.CompletedAt = DateTimeOffset.UtcNow;
        }

        if (!string.IsNullOrWhiteSpace(gatewayReference))
        {
            entity.Payment.GatewayReference = gatewayReference;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.Payment.ToModel();
    }

    public async Task<RegistrationNotification> AddParticipantNotificationAsync(
        Guid participantId,
        RegistrationNotification notification,
        CancellationToken cancellationToken = default)
    {
        var entity = notification.ToEntity();
        entity.ParticipantRegistrationId = participantId;
        _dbContext.RegistrationNotifications.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    public async Task<BusinessPlanReview> AddBusinessPlanReviewAsync(
        Guid participantId,
        BusinessPlanReview review,
        CancellationToken cancellationToken = default)
    {
        var entity = review.ToEntity();
        entity.ParticipantRegistrationId = participantId;
        entity.CreatedAt = DateTimeOffset.UtcNow;
        _dbContext.BusinessPlanReviews.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    public async Task<IReadOnlyCollection<BusinessPlanReview>> ListBusinessPlanReviewsAsync(
        Guid participantId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.BusinessPlanReviews
            .AsNoTracking()
            .Where(r => r.ParticipantRegistrationId == participantId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(r => r.ToModel()).ToList();
    }

    public async Task<BusinessPlanReview?> GetBusinessPlanReviewAsync(
        Guid participantId,
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.BusinessPlanReviews
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.ParticipantRegistrationId == participantId && r.Id == reviewId, cancellationToken);

        return entity?.ToModel();
    }

    public async Task<JudgeRegistration> AddJudgeAsync(JudgeRegistration registration, CancellationToken cancellationToken = default)
    {
        var entity = registration.ToEntity();
        _dbContext.JudgeRegistrations.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    public async Task<JudgeRegistration?> GetJudgeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.JudgeRegistrations
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity?.ToModel();
    }

    public async Task<IReadOnlyCollection<JudgeRegistration>> ListJudgesAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.JudgeRegistrations
            .AsNoTracking()
            .OrderByDescending(x => x.SubmittedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return entities.Select(x => x.ToModel()).ToList();
    }

    public async Task<InvestorRegistration> AddInvestorAsync(InvestorRegistration registration, CancellationToken cancellationToken = default)
    {
        var entity = registration.ToEntity();
        _dbContext.InvestorRegistrations.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    public async Task<InvestorRegistration?> GetInvestorAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.InvestorRegistrations
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity?.ToModel();
    }

    public async Task<IReadOnlyCollection<InvestorRegistration>> ListInvestorsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.InvestorRegistrations
            .AsNoTracking()
            .OrderByDescending(x => x.SubmittedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return entities.Select(x => x.ToModel()).ToList();
    }
}
