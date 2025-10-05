using Microsoft.EntityFrameworkCore;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
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
            .OrderByDescending(x => x.SubmittedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return entities.Select(x => x.ToModel()).ToList();
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
