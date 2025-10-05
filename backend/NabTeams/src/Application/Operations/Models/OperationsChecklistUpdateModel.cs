using NabTeams.Domain.Enums;

namespace NabTeams.Application.Operations.Models;

public record OperationsChecklistUpdateModel(
    OperationsChecklistStatus Status,
    string? Notes,
    string? ArtifactUrl
);
