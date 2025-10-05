using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NabTeams.Application.Events;

public class EventService : IEventService
{
    private readonly IEventRepository _repository;

    public EventService(IEventRepository repository)
    {
        _repository = repository;
    }

    public Task<EventDetail> CreateAsync(EventDetail model, CancellationToken cancellationToken = default)
        => _repository.CreateAsync(model, cancellationToken);

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);

    public Task<EventDetail?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetAsync(id, cancellationToken);

    public Task<IReadOnlyCollection<EventDetail>> ListAsync(CancellationToken cancellationToken = default)
        => _repository.ListAsync(cancellationToken);

    public Task<EventDetail?> UpdateAsync(Guid id, EventDetail model, CancellationToken cancellationToken = default)
    {
        if (id != model.Id && model.Id != Guid.Empty)
        {
            model = model with { Id = id };
        }

        return _repository.UpdateAsync(model with { Id = id }, cancellationToken);
    }
}
