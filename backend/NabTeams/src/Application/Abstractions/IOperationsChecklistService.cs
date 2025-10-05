using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NabTeams.Application.Operations.Models;

namespace NabTeams.Application.Abstractions;

public interface IOperationsChecklistService
{
    Task<IReadOnlyList<OperationsChecklistItemModel>> ListAsync(CancellationToken cancellationToken = default);
    Task<OperationsChecklistItemModel> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OperationsChecklistItemModel> UpdateAsync(Guid id, OperationsChecklistUpdateModel update, CancellationToken cancellationToken = default);
}
