using NabTeams.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NabTeams.Application.Abstractions;

public interface IEventRepository
{
    Task<EventDetail> CreateAsync(EventDetail model, CancellationToken cancellationToken = default);
    Task<EventDetail?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<EventDetail>> ListAsync(CancellationToken cancellationToken = default);
    Task<EventDetail?> UpdateAsync(EventDetail model, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
