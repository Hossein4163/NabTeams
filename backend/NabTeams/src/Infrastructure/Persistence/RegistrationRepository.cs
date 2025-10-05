using Microsoft.EntityFrameworkCore;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
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
