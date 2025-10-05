using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NabTeams.Domain.Entities;

namespace NabTeams.Application.Abstractions;

public record BusinessPlanAnalysisRequest(
    Guid ParticipantRegistrationId,
    string Narrative,
    IReadOnlyCollection<string> AttachmentUrls,
    string? AdditionalContext);

public interface IBusinessPlanAnalyzer
{
    Task<BusinessPlanReview> AnalyzeAsync(BusinessPlanAnalysisRequest request, CancellationToken cancellationToken = default);
}
