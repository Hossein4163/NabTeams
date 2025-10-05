using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NabTeams.Domain.Entities;

namespace NabTeams.Application.Abstractions;

public interface IOperationsChecklistRepository
{
    Task<IReadOnlyList<OperationsChecklistItemEntity>> ListAsync(CancellationToken cancellationToken = default);
    Task<OperationsChecklistItemEntity?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OperationsChecklistItemEntity> UpdateAsync(OperationsChecklistItemEntity entity, CancellationToken cancellationToken = default);
}
