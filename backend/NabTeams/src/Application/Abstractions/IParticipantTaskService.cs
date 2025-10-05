using NabTeams.Application.Tasks.Models;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NabTeams.Application.Abstractions;

public interface IParticipantTaskService
{
    Task<IReadOnlyCollection<ParticipantTask>> ListAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<ParticipantTask> CreateAsync(Guid participantId, ParticipantTaskInput input, CancellationToken cancellationToken = default);
    Task<ParticipantTask?> UpdateAsync(Guid taskId, ParticipantTaskInput input, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<ParticipantTask?> UpdateStatusAsync(Guid taskId, ParticipantTaskStatus status, CancellationToken cancellationToken = default);
    Task<TaskAdviceResult> GenerateAdviceAsync(Guid participantId, TaskAdviceRequest request, CancellationToken cancellationToken = default);
}
