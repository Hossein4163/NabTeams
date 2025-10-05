namespace NabTeams.Domain.Enums;

public enum RegistrationDocumentCategory
{
    ProjectArchive,
    TeamResume,
    Presentation,
    BusinessModel,
    Other
}

public enum RegistrationLinkType
{
    LinkedIn,
    GitHub,
    Website,
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
