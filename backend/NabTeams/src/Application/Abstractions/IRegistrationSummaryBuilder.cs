using System.Threading;
using System.Threading.Tasks;
using NabTeams.Domain.Entities;

namespace NabTeams.Application.Abstractions;

public interface IRegistrationSummaryBuilder
{
    Task<StoredRegistrationDocument> BuildSummaryAsync(
        ParticipantRegistration registration,
        CancellationToken cancellationToken = default);
}
