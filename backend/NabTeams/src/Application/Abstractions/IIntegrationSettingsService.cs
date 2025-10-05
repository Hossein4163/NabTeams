using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NabTeams.Application.Common;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Application.Abstractions;

public interface IIntegrationSettingsService
{
    Task<IReadOnlyCollection<IntegrationSetting>> ListAsync(
        IntegrationProviderType? type,
        CancellationToken cancellationToken = default);

    Task<IntegrationSetting?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IntegrationSetting> UpsertAsync(
        IntegrationSetting setting,
        bool activate,
        CancellationToken cancellationToken = default);

    Task SetActiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<GeminiOptions> GetGeminiOptionsAsync(CancellationToken cancellationToken = default);

    Task<(IntegrationSetting Setting, PaymentGatewayOptions Options)?> GetPaymentGatewayAsync(
        CancellationToken cancellationToken = default);

    Task<NotificationOptions> GetNotificationOptionsAsync(CancellationToken cancellationToken = default);
}
