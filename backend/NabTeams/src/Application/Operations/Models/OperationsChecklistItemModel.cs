using System;
using NabTeams.Domain.Enums;

namespace NabTeams.Application.Operations.Models;

public record OperationsChecklistItemModel(
    Guid Id,
    string Key,
    string Title,
    string Description,
    string Category,
    OperationsChecklistStatus Status,
    DateTimeOffset? CompletedAt,
    string? Notes,
    string? ArtifactUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);
