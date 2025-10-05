using System;
using NabTeams.Domain.Enums;

namespace NabTeams.Domain.Entities;

public record IntegrationSetting
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public IntegrationProviderType Type { get; init; }
        = IntegrationProviderType.Gemini;
    public string ProviderKey { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Configuration { get; init; } = string.Empty;
    public bool IsActive { get; init; }
        = false;
    public DateTimeOffset CreatedAt { get; init; }
        = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; init; }
        = DateTimeOffset.UtcNow;
}
