using NabTeams.Domain.Enums;

namespace NabTeams.Domain.Entities;

public enum RegistrationStatus
{
    Draft,
    Submitted,
    UnderReview,
    Approved,
    Rejected
}

public enum ParticipantRegistrationStage
{
    HeadDetails,
    TeamDetails,
    Documents,
    ReadyForReview,
    Completed
}

public class ParticipantRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Draft;
    public ParticipantRegistrationStage Stage { get; set; } = ParticipantRegistrationStage.HeadDetails;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubmittedAt { get; set; }
        = null;
    public DateTimeOffset? ApprovedAt { get; set; }
        = null;
    public string HeadFullName { get; set; } = string.Empty;
    public string HeadNationalId { get; set; } = string.Empty;
    public string HeadPhoneNumber { get; set; } = string.Empty;
    public DateTime? HeadBirthDate { get; set; }
        = null;
    public string HeadDegree { get; set; } = string.Empty;
    public string HeadMajor { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public bool HasTeam { get; set; }
        = false;
    public bool TeamCompleted { get; set; }
        = false;
    public List<TeamMember> TeamMembers { get; } = new();
    public string? ProjectFileUrl { get; set; }
        = null;
    public string? ResumeFileUrl { get; set; }
        = null;
    public List<string> SocialLinks { get; set; } = new();
    public string? FinalSummary { get; set; }
        = null;
    public string? JudgeNotes { get; set; }
        = null;
    public Guid? WorkspaceId { get; set; }
        = null;
}

public class TeamMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FocusArea { get; set; } = string.Empty;
}

public class JudgeRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
        = null;
    public string ExpertiseArea { get; set; } = string.Empty;
    public string HighestDegree { get; set; } = string.Empty;
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Draft;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubmittedAt { get; set; }
        = null;
    public DateTimeOffset? ApprovedAt { get; set; }
        = null;
    public string? Notes { get; set; }
        = null;
}

public class InvestorRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public RegistrationStatus Status { get; set; } = RegistrationStatus.Draft;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? SubmittedAt { get; set; }
        = null;
    public DateTimeOffset? ApprovedAt { get; set; }
        = null;
    public List<string> InterestAreas { get; set; } = new();
    public string? Notes { get; set; }
        = null;
}

public enum ProjectTaskStatus
{
    Planned,
    InProgress,
    Blocked,
    Completed
}

public enum StaffingRequestStatus
{
    Open,
    InNegotiation,
    Closed
}

public enum AiInsightType
{
    Design,
    BusinessModel,
    PitchDeck
}

public class ProjectWorkspace
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ParticipantRegistrationId { get; set; }
        = Guid.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string Vision { get; set; } = string.Empty;
    public string? BusinessModelSummary { get; set; }
        = null;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<ProjectTask> Tasks { get; } = new();
    public List<StaffingRequest> StaffingRequests { get; } = new();
    public List<AiInsight> Insights { get; } = new();
}

public class ProjectTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
        = Guid.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
        = null;
    public string Assignee { get; set; } = string.Empty;
    public ProjectTaskStatus Status { get; set; } = ProjectTaskStatus.Planned;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
        = null;
}

public class StaffingRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
        = Guid.Empty;
    public string Skill { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPaidOpportunity { get; set; }
        = false;
    public StaffingRequestStatus Status { get; set; } = StaffingRequestStatus.Open;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class AiInsight
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
        = Guid.Empty;
    public AiInsightType Type { get; set; } = AiInsightType.BusinessModel;
    public string Summary { get; set; } = string.Empty;
    public string ImprovementAreas { get; set; } = string.Empty;
    public double Confidence { get; set; }
        = 0.0d;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum PaymentStatus
{
    Pending,
    AwaitingConfirmation,
    Paid,
    Failed
}

public class PaymentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RegistrationId { get; set; } = Guid.Empty;
    public string RegistrationType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
        = 0m;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? GatewayUrl { get; set; }
        = null;
    public string? ReferenceCode { get; set; }
        = null;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PaidAt { get; set; }
        = null;
    public string? Notes { get; set; }
        = null;
}

public class NotificationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RegistrationId { get; set; } = Guid.Empty;
    public string RegistrationType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
}
