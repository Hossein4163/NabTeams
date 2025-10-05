using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NabTeams.Application.Abstractions;

public interface IParticipantTaskRepository
{
    Task<ParticipantTask> CreateAsync(ParticipantTask task, CancellationToken cancellationToken = default);
    Task<ParticipantTask?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ParticipantTask>> ListForParticipantAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<ParticipantTask?> UpdateAsync(Guid id, ParticipantTask task, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ParticipantTask?> UpdateStatusAsync(Guid id, ParticipantTaskStatus status, string? aiRecommendation, CancellationToken cancellationToken = default);
}
