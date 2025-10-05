namespace NabTeams.Domain.Enums;

public enum RegistrationDocumentCategory
{
    ProjectArchive,
    TeamResume,
    Presentation,
    BusinessModel,
    BusinessPlan,
    PitchDeck,
    Resume,
    Other
}

public enum RegistrationLinkType
{
    LinkedIn,
    GitHub,
    Website,
    Instagram,
    Demo,
    Other
}

public enum RegistrationStatus
{
    Submitted,
    Finalized,
    Approved,
    PaymentRequested,
    PaymentCompleted,
    Rejected,
    Cancelled
}

public enum RegistrationPaymentStatus
{
    Pending,
    Completed,
    Failed,
    Cancelled
}

public enum NotificationChannel
{
    Email,
    Sms
}

public enum BusinessPlanReviewStatus
{
    Pending,
    Completed,
    Failed
}
