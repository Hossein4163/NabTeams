using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<MessageEntity> Messages => Set<MessageEntity>();
    public DbSet<ModerationLogEntity> ModerationLogs => Set<ModerationLogEntity>();
    public DbSet<UserDisciplineEntity> UserDisciplines => Set<UserDisciplineEntity>();
    public DbSet<DisciplineEventEntity> DisciplineEvents => Set<DisciplineEventEntity>();
    public DbSet<KnowledgeBaseItemEntity> KnowledgeBaseItems => Set<KnowledgeBaseItemEntity>();
    public DbSet<AppealEntity> Appeals => Set<AppealEntity>();
    public DbSet<ParticipantRegistrationEntity> ParticipantRegistrations => Set<ParticipantRegistrationEntity>();
    public DbSet<TeamMemberEntity> TeamMembers => Set<TeamMemberEntity>();
    public DbSet<JudgeRegistrationEntity> JudgeRegistrations => Set<JudgeRegistrationEntity>();
    public DbSet<InvestorRegistrationEntity> InvestorRegistrations => Set<InvestorRegistrationEntity>();
    public DbSet<ProjectWorkspaceEntity> ProjectWorkspaces => Set<ProjectWorkspaceEntity>();
    public DbSet<ProjectTaskEntity> ProjectTasks => Set<ProjectTaskEntity>();
    public DbSet<StaffingRequestEntity> StaffingRequests => Set<StaffingRequestEntity>();
    public DbSet<AiInsightEntity> AiInsights => Set<AiInsightEntity>();
    public DbSet<PaymentRecordEntity> PaymentRecords => Set<PaymentRecordEntity>();
    public DbSet<NotificationLogEntity> NotificationLogs => Set<NotificationLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var channelConverter = new EnumToStringConverter<RoleChannel>();
        var statusConverter = new EnumToStringConverter<MessageStatus>();
        var appealStatusConverter = new EnumToStringConverter<AppealStatus>();
        var registrationStatusConverter = new EnumToStringConverter<RegistrationStatus>();
        var participantStageConverter = new EnumToStringConverter<ParticipantRegistrationStage>();
        var taskStatusConverter = new EnumToStringConverter<ProjectTaskStatus>();
        var staffingStatusConverter = new EnumToStringConverter<StaffingRequestStatus>();
        var insightTypeConverter = new EnumToStringConverter<AiInsightType>();
        var paymentStatusConverter = new EnumToStringConverter<PaymentStatus>();

        var stringListComparer = new ValueComparer<List<string>>(
            (left, right) => (left ?? new()).SequenceEqual(right ?? new()),
            list => (list ?? new()).Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
            list => (list ?? new()).ToList());

        modelBuilder.Entity<MessageEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Channel).HasConversion(channelConverter).IsRequired();
            entity.Property(e => e.Status).HasConversion(statusConverter).IsRequired();
            entity.Property(e => e.SenderUserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.ModerationTags)
                .HasConversion(
                    list => string.Join('\u001F', list ?? new()),
                    value => string.IsNullOrWhiteSpace(value)
                        ? new List<string>()
                        : value.Split('\u001F', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList())
                .Metadata.SetValueComparer(stringListComparer);
        });

        modelBuilder.Entity<ModerationLogEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Channel).HasConversion(channelConverter).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.PolicyTags)
                .HasConversion(
                    list => string.Join('\u001F', list ?? new()),
                    value => string.IsNullOrWhiteSpace(value)
                        ? new List<string>()
                        : value.Split('\u001F', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList())
                .Metadata.SetValueComparer(stringListComparer);
        });

        modelBuilder.Entity<UserDisciplineEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Channel).HasConversion(channelConverter).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.Channel }).IsUnique();
            entity.HasMany(e => e.Events)
                .WithOne(e => e.UserDiscipline)
                .HasForeignKey(e => e.UserDisciplineId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DisciplineEventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(256);
        });

        modelBuilder.Entity<KnowledgeBaseItemEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Audience).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Tags)
                .HasConversion(
                    list => string.Join('\u001F', list ?? new()),
                    value => string.IsNullOrWhiteSpace(value)
                        ? new List<string>()
                        : value.Split('\u001F', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList())
                .Metadata.SetValueComparer(stringListComparer);
        });

        modelBuilder.Entity<AppealEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Channel).HasConversion(channelConverter).IsRequired();
            entity.Property(e => e.Status).HasConversion(appealStatusConverter).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(512).IsRequired();
            entity.HasIndex(e => new { e.MessageId, e.UserId }).IsUnique();
        });

        modelBuilder.Entity<ParticipantRegistrationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Status).HasConversion(registrationStatusConverter).IsRequired();
            entity.Property(e => e.Stage).HasConversion(participantStageConverter).IsRequired();
            entity.Property(e => e.HeadFullName).HasMaxLength(128);
            entity.Property(e => e.HeadNationalId).HasMaxLength(32);
            entity.Property(e => e.HeadPhoneNumber).HasMaxLength(32);
            entity.Property(e => e.TeamName).HasMaxLength(128);
            entity.Property(e => e.SocialLinks)
                .HasConversion(
                    list => string.Join('\u001F', list ?? new()),
                    value => string.IsNullOrWhiteSpace(value)
                        ? new List<string>()
                        : value.Split('\u001F', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList())
                .Metadata.SetValueComparer(stringListComparer);

            entity.HasMany(e => e.TeamMembers)
                .WithOne(m => m.Participant)
                .HasForeignKey(m => m.ParticipantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeamMemberEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).HasMaxLength(128);
            entity.Property(e => e.Role).HasMaxLength(64);
            entity.Property(e => e.FocusArea).HasMaxLength(128);
        });

        modelBuilder.Entity<JudgeRegistrationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Status).HasConversion(registrationStatusConverter).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(128);
            entity.Property(e => e.NationalId).HasMaxLength(32);
            entity.Property(e => e.PhoneNumber).HasMaxLength(32);
        });

        modelBuilder.Entity<InvestorRegistrationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Status).HasConversion(registrationStatusConverter).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(128);
            entity.Property(e => e.NationalId).HasMaxLength(32);
            entity.Property(e => e.PhoneNumber).HasMaxLength(32);
            entity.Property(e => e.InterestAreas)
                .HasConversion(
                    list => string.Join('\u001F', list ?? new()),
                    value => string.IsNullOrWhiteSpace(value)
                        ? new List<string>()
                        : value.Split('\u001F', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList())
                .Metadata.SetValueComparer(stringListComparer);
        });

        modelBuilder.Entity<ProjectWorkspaceEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProjectName).HasMaxLength(128);
            entity.Property(e => e.Vision).HasMaxLength(512);
            entity.HasMany(e => e.Tasks)
                .WithOne(t => t.Workspace)
                .HasForeignKey(t => t.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.StaffingRequests)
                .WithOne(s => s.Workspace)
                .HasForeignKey(s => s.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Insights)
                .WithOne(i => i.Workspace)
                .HasForeignKey(i => i.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectTaskEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(128);
            entity.Property(e => e.Status).HasConversion(taskStatusConverter).IsRequired();
        });

        modelBuilder.Entity<StaffingRequestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Skill).HasMaxLength(128);
            entity.Property(e => e.Status).HasConversion(staffingStatusConverter).IsRequired();
        });

        modelBuilder.Entity<AiInsightEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion(insightTypeConverter).IsRequired();
        });

        modelBuilder.Entity<PaymentRecordEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RegistrationType).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Status).HasConversion(paymentStatusConverter).IsRequired();
        });

        modelBuilder.Entity<NotificationLogEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RegistrationType).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Channel).HasMaxLength(32).IsRequired();
        });
    }
}

public class MessageEntity
{
    public Guid Id { get; set; }
    public RoleChannel Channel { get; set; }
    public string SenderUserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public MessageStatus Status { get; set; }
    public List<string> ModerationTags { get; set; } = new();
    public string? ModerationNotes { get; set; }
    public int PenaltyPoints { get; set; }
}

public class ModerationLogEntity
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public RoleChannel Channel { get; set; }
    public double RiskScore { get; set; }
    public List<string> PolicyTags { get; set; } = new();
    public string ActionTaken { get; set; } = string.Empty;
    public int PenaltyPoints { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class UserDisciplineEntity
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public RoleChannel Channel { get; set; }
    public int ScoreBalance { get; set; }
    public List<DisciplineEventEntity> Events { get; set; } = new();
}

public class DisciplineEventEntity
{
    public Guid Id { get; set; }
    public Guid UserDisciplineId { get; set; }
    public UserDisciplineEntity? UserDiscipline { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int Delta { get; set; }
    public Guid MessageId { get; set; }
}

public class KnowledgeBaseItemEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Audience { get; set; } = "all";
    public List<string> Tags { get; set; } = new();
    public DateTimeOffset UpdatedAt { get; set; }
}

public class AppealEntity
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public RoleChannel Channel { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
    public AppealStatus Status { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
}

public class ParticipantRegistrationEntity
{
    public Guid Id { get; set; }
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
    public string? ProjectFileUrl { get; set; }
        = null;
    public string? ResumeFileUrl { get; set; }
        = null;
    public string? FinalSummary { get; set; }
        = null;
    public string? JudgeNotes { get; set; }
        = null;
    public Guid? WorkspaceId { get; set; }
        = null;
    public List<string> SocialLinks { get; set; } = new();
    public ICollection<TeamMemberEntity> TeamMembers { get; set; } = new List<TeamMemberEntity>();
}

public class TeamMemberEntity
{
    public Guid Id { get; set; }
    public Guid ParticipantId { get; set; }
        = Guid.Empty;
    public ParticipantRegistrationEntity? Participant { get; set; }
        = null;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FocusArea { get; set; } = string.Empty;
}

public class JudgeRegistrationEntity
{
    public Guid Id { get; set; }
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

public class InvestorRegistrationEntity
{
    public Guid Id { get; set; }
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

public class ProjectWorkspaceEntity
{
    public Guid Id { get; set; }
    public Guid ParticipantRegistrationId { get; set; }
        = Guid.Empty;
    public ParticipantRegistrationEntity? Participant { get; set; }
        = null;
    public string ProjectName { get; set; } = string.Empty;
    public string Vision { get; set; } = string.Empty;
    public string? BusinessModelSummary { get; set; }
        = null;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<ProjectTaskEntity> Tasks { get; set; } = new List<ProjectTaskEntity>();
    public ICollection<StaffingRequestEntity> StaffingRequests { get; set; } = new List<StaffingRequestEntity>();
    public ICollection<AiInsightEntity> Insights { get; set; } = new List<AiInsightEntity>();
}

public class ProjectTaskEntity
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
        = Guid.Empty;
    public ProjectWorkspaceEntity? Workspace { get; set; }
        = null;
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

public class StaffingRequestEntity
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
        = Guid.Empty;
    public ProjectWorkspaceEntity? Workspace { get; set; }
        = null;
    public string Skill { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPaidOpportunity { get; set; }
        = false;
    public StaffingRequestStatus Status { get; set; } = StaffingRequestStatus.Open;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class AiInsightEntity
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
        = Guid.Empty;
    public ProjectWorkspaceEntity? Workspace { get; set; }
        = null;
    public AiInsightType Type { get; set; } = AiInsightType.BusinessModel;
    public string Summary { get; set; } = string.Empty;
    public string ImprovementAreas { get; set; } = string.Empty;
    public double Confidence { get; set; }
        = 0.0d;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class PaymentRecordEntity
{
    public Guid Id { get; set; }
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

public class NotificationLogEntity
{
    public Guid Id { get; set; }
    public Guid RegistrationId { get; set; } = Guid.Empty;
    public string RegistrationType { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
}
