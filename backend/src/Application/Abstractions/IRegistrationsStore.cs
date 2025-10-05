using NabTeams.Domain.Entities;

namespace NabTeams.Application.Abstractions;

public interface IParticipantRegistrationStore
{
    Task<ParticipantRegistration?> GetByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<ParticipantRegistration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ParticipantRegistration>> ListAsync(RegistrationStatus? status, CancellationToken cancellationToken = default);
    Task<ParticipantRegistration> SaveAsync(ParticipantRegistration registration, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid id, RegistrationStatus status, string? judgeNotes, CancellationToken cancellationToken = default);
}

public interface IJudgeRegistrationStore
{
    Task<JudgeRegistration?> GetByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<JudgeRegistration>> ListAsync(RegistrationStatus? status, CancellationToken cancellationToken = default);
    Task<JudgeRegistration> SaveAsync(JudgeRegistration registration, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid id, RegistrationStatus status, string? notes, CancellationToken cancellationToken = default);
}

public interface IInvestorRegistrationStore
{
    Task<InvestorRegistration?> GetByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<InvestorRegistration>> ListAsync(RegistrationStatus? status, CancellationToken cancellationToken = default);
    Task<InvestorRegistration> SaveAsync(InvestorRegistration registration, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid id, RegistrationStatus status, string? notes, CancellationToken cancellationToken = default);
}

public interface IProjectWorkspaceStore
{
    Task<ProjectWorkspace?> GetByParticipantAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<ProjectWorkspace> SaveAsync(ProjectWorkspace workspace, CancellationToken cancellationToken = default);
    Task<ProjectTask> SaveTaskAsync(Guid workspaceId, ProjectTask task, CancellationToken cancellationToken = default);
    Task UpdateTaskStatusAsync(Guid workspaceId, Guid taskId, ProjectTaskStatus status, CancellationToken cancellationToken = default);
    Task<StaffingRequest> AddStaffingRequestAsync(Guid workspaceId, StaffingRequest request, CancellationToken cancellationToken = default);
    Task UpdateStaffingStatusAsync(Guid workspaceId, Guid requestId, StaffingRequestStatus status, CancellationToken cancellationToken = default);
    Task<AiInsight> AddInsightAsync(Guid workspaceId, AiInsight insight, CancellationToken cancellationToken = default);
}

public interface IPostApprovalStore
{
    Task<PaymentRecord> CreateOrUpdatePaymentAsync(PaymentRecord record, CancellationToken cancellationToken = default);
    Task<PaymentRecord?> GetPaymentAsync(Guid registrationId, string registrationType, CancellationToken cancellationToken = default);
    Task<NotificationLog> LogNotificationAsync(NotificationLog log, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<NotificationLog>> GetNotificationsAsync(Guid registrationId, string registrationType, CancellationToken cancellationToken = default);
}
