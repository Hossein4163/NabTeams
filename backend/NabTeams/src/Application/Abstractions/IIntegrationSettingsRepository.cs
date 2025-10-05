using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Application.Abstractions;

public interface IIntegrationSettingsRepository
{
    Task<IReadOnlyCollection<IntegrationSetting>> ListAsync(
        IntegrationProviderType? type,
        CancellationToken cancellationToken = default);

    Task<IntegrationSetting?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IntegrationSetting?> GetActiveAsync(
        IntegrationProviderType type,
        CancellationToken cancellationToken = default);

    Task<IntegrationSetting> UpsertAsync(
        IntegrationSetting setting,
        CancellationToken cancellationToken = default);

    Task SetActiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
