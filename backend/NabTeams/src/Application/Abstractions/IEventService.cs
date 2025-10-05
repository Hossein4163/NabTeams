using NabTeams.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NabTeams.Application.Abstractions;

public interface IEventService
{
    Task<IReadOnlyCollection<EventDetail>> ListAsync(CancellationToken cancellationToken = default);
    Task<EventDetail?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EventDetail> CreateAsync(EventDetail model, CancellationToken cancellationToken = default);
    Task<EventDetail?> UpdateAsync(Guid id, EventDetail model, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
