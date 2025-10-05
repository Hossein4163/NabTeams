using NabTeams.Domain.Entities;

namespace NabTeams.Application.Common;

public record SendMessageRequest
{
    public const int MaxContentLength = 2000;

    public string Content { get; init; } = string.Empty;
}

public record SendMessageResponse
{
    public Guid MessageId { get; init; }
    public MessageStatus Status { get; init; }
    public double ModerationRisk { get; init; }
    public IReadOnlyCollection<string> ModerationTags { get; init; } = Array.Empty<string>();
    public string? ModerationNotes { get; init; }
    public int PenaltyPoints { get; init; }
    public bool SoftWarn { get; init; }
    public string? RateLimitMessage { get; init; }
}

public record MessagesResponse
{
    public IReadOnlyCollection<Message> Messages { get; init; } = Array.Empty<Message>();
}

public record KnowledgeBaseUpsertRequest
{
    public string? Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public IReadOnlyCollection<string>? Tags { get; init; }
}

public record CreateAppealRequest
{
    public Guid MessageId { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public record AppealDecisionRequest
{
    public AppealStatus Status { get; init; }
    public string? Notes { get; init; }
}

public record ParticipantHeadStepRequest
{
    public string FullName { get; init; } = string.Empty;
    public string NationalId { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public DateTime? BirthDate { get; init; }
        = null;
    public string Degree { get; init; } = string.Empty;
    public string Major { get; init; } = string.Empty;
    public string TeamName { get; init; } = string.Empty;
    public bool HasTeam { get; init; }
        = false;
}

public record ParticipantTeamStepRequest
{
    public bool TeamCompleted { get; init; }
        = false;
    public IReadOnlyCollection<TeamMemberRequest> Members { get; init; } = Array.Empty<TeamMemberRequest>();
}

public record TeamMemberRequest
{
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string FocusArea { get; init; } = string.Empty;
}

public record ParticipantDocumentsStepRequest
{
    public string? ProjectFileUrl { get; init; }
        = null;
    public string? ResumeFileUrl { get; init; }
        = null;
    public IReadOnlyCollection<string>? SocialLinks { get; init; }
        = null;
}

public record ParticipantSubmitRequest
{
    public string FinalSummary { get; init; } = string.Empty;
}

public record RegistrationStatusRequest
{
    public RegistrationStatus Status { get; init; } = RegistrationStatus.UnderReview;
    public string? Notes { get; init; }
        = null;
}

public record JudgeRegistrationRequest
{
    public string FullName { get; init; } = string.Empty;
    public string NationalId { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public DateTime? BirthDate { get; init; }
        = null;
    public string ExpertiseArea { get; init; } = string.Empty;
    public string HighestDegree { get; init; } = string.Empty;
}

public record InvestorRegistrationRequest
{
    public string FullName { get; init; } = string.Empty;
    public string NationalId { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public IReadOnlyCollection<string>? InterestAreas { get; init; }
        = null;
}

public record ProjectWorkspaceRequest
{
    public string ProjectName { get; init; } = string.Empty;
    public string Vision { get; init; } = string.Empty;
    public string? BusinessModelSummary { get; init; }
        = null;
}

public record ProjectTaskRequest
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime? DueDate { get; init; }
        = null;
    public string Assignee { get; init; } = string.Empty;
}

public record StaffingRequestCreate
{
    public string Skill { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsPaidOpportunity { get; init; }
        = false;
}

public record AiInsightRequest
{
    public AiInsightType Type { get; init; } = AiInsightType.BusinessModel;
    public string Summary { get; init; } = string.Empty;
    public string ImprovementAreas { get; init; } = string.Empty;
    public double Confidence { get; init; }
        = 0.0d;
}

public record PaymentInstructionRequest
{
    public decimal Amount { get; init; }
        = 0m;
    public string? GatewayUrl { get; init; }
        = null;
}

public record PaymentStatusUpdateRequest
{
    public PaymentStatus Status { get; init; } = PaymentStatus.Pending;
    public string? ReferenceCode { get; init; }
        = null;
}

public record NotificationLogRequest
{
    public string Channel { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
