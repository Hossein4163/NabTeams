using NabTeams.Domain.Entities;

namespace NabTeams.Application.Abstractions;

public interface IRegistrationRepository
{
    Task<ParticipantRegistration> AddParticipantAsync(ParticipantRegistration registration, CancellationToken cancellationToken = default);
    Task<ParticipantRegistration?> GetParticipantAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ParticipantRegistration>> ListParticipantsAsync(CancellationToken cancellationToken = default);

    Task<JudgeRegistration> AddJudgeAsync(JudgeRegistration registration, CancellationToken cancellationToken = default);
    Task<JudgeRegistration?> GetJudgeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<JudgeRegistration>> ListJudgesAsync(CancellationToken cancellationToken = default);

    Task<InvestorRegistration> AddInvestorAsync(InvestorRegistration registration, CancellationToken cancellationToken = default);
    Task<InvestorRegistration?> GetInvestorAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<InvestorRegistration>> ListInvestorsAsync(CancellationToken cancellationToken = default);
}
