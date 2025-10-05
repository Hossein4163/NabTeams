using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using System.Linq;

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
    public DbSet<JudgeRegistrationEntity> JudgeRegistrations => Set<JudgeRegistrationEntity>();
    public DbSet<InvestorRegistrationEntity> InvestorRegistrations => Set<InvestorRegistrationEntity>();
    public DbSet<RegistrationPaymentEntity> RegistrationPayments => Set<RegistrationPaymentEntity>();
    public DbSet<RegistrationNotificationEntity> RegistrationNotifications => Set<RegistrationNotificationEntity>();
    public DbSet<BusinessPlanReviewEntity> BusinessPlanReviews => Set<BusinessPlanReviewEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var channelConverter = new EnumToStringConverter<RoleChannel>();
        var statusConverter = new EnumToStringConverter<MessageStatus>();
        var appealStatusConverter = new EnumToStringConverter<AppealStatus>();
        var documentCategoryConverter = new EnumToStringConverter<RegistrationDocumentCategory>();
        var linkTypeConverter = new EnumToStringConverter<RegistrationLinkType>();
        var registrationStatusConverter = new EnumToStringConverter<RegistrationStatus>();
        var paymentStatusConverter = new EnumToStringConverter<RegistrationPaymentStatus>();
        var notificationChannelConverter = new EnumToStringConverter<NotificationChannel>();
        var businessPlanStatusConverter = new EnumToStringConverter<BusinessPlanReviewStatus>();

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
            entity.Property(e => e.HeadFirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.HeadLastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.NationalId).HasMaxLength(16).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(128);
            entity.Property(e => e.EducationDegree).HasMaxLength(128).IsRequired();
            entity.Property(e => e.FieldOfStudy).HasMaxLength(128).IsRequired();
            entity.Property(e => e.TeamName).HasMaxLength(128).IsRequired();
            entity.Property(e => e.AdditionalNotes).HasMaxLength(1024);
            entity.Property(e => e.SubmittedAt).IsRequired();
            entity.Property(e => e.Status).HasConversion(registrationStatusConverter).IsRequired();
            entity.Property(e => e.SummaryFileUrl).HasMaxLength(512);

            entity.HasMany(e => e.Members)
                .WithOne(e => e.ParticipantRegistration)
                .HasForeignKey(e => e.ParticipantRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Documents)
                .WithOne(e => e.ParticipantRegistration)
                .HasForeignKey(e => e.ParticipantRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Links)
                .WithOne(e => e.ParticipantRegistration)
                .HasForeignKey(e => e.ParticipantRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Payment)
                .WithOne(e => e.ParticipantRegistration)
                .HasForeignKey<RegistrationPaymentEntity>(e => e.ParticipantRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Notifications)
                .WithOne(e => e.ParticipantRegistration)
                .HasForeignKey(e => e.ParticipantRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.BusinessPlanReviews)
                .WithOne(e => e.ParticipantRegistration)
                .HasForeignKey(e => e.ParticipantRegistrationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeamMemberEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(100).IsRequired();
            entity.Property(e => e.FocusArea).HasMaxLength(150).IsRequired();
        });

        modelBuilder.Entity<RegistrationDocumentEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).HasConversion(documentCategoryConverter).IsRequired();
            entity.Property(e => e.FileName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.FileUrl).HasMaxLength(512).IsRequired();
        });

        modelBuilder.Entity<RegistrationLinkEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion(linkTypeConverter).IsRequired();
            entity.Property(e => e.Label).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Url).HasMaxLength(512).IsRequired();
        });

        modelBuilder.Entity<RegistrationPaymentEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(16).IsRequired();
            entity.Property(e => e.PaymentUrl).HasMaxLength(512).IsRequired();
            entity.Property(e => e.Status).HasConversion(paymentStatusConverter).IsRequired();
            entity.Property(e => e.GatewayReference).HasMaxLength(128);
            entity.Property(e => e.RequestedAt).IsRequired();
        });

        modelBuilder.Entity<RegistrationNotificationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Channel).HasConversion(notificationChannelConverter).IsRequired();
            entity.Property(e => e.Recipient).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Subject).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.SentAt).IsRequired();
        });

        modelBuilder.Entity<BusinessPlanReviewEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion(businessPlanStatusConverter).IsRequired();
            entity.Property(e => e.Summary).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.Strengths).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.Risks).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.Recommendations).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.RawResponse).HasMaxLength(8000);
            entity.Property(e => e.Model).HasMaxLength(128).IsRequired();
            entity.Property(e => e.SourceDocumentUrl).HasMaxLength(512);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<JudgeRegistrationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.NationalId).HasMaxLength(16).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(128);
            entity.Property(e => e.FieldOfExpertise).HasMaxLength(256).IsRequired();
            entity.Property(e => e.HighestDegree).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Biography).HasMaxLength(1024);
            entity.Property(e => e.SubmittedAt).IsRequired();
            entity.Property(e => e.Status).HasConversion(registrationStatusConverter).IsRequired();
        });

        modelBuilder.Entity<InvestorRegistrationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.NationalId).HasMaxLength(16).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(32).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(128);
            entity.Property(e => e.InterestAreas)
                .HasConversion(
                    list => string.Join('\u001F', list ?? new()),
                    value => string.IsNullOrWhiteSpace(value)
                        ? new List<string>()
                        : value.Split('\u001F', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList())
                .Metadata.SetValueComparer(stringListComparer);
            entity.Property(e => e.AdditionalNotes).HasMaxLength(1024);
            entity.Property(e => e.SubmittedAt).IsRequired();
            entity.Property(e => e.Status).HasConversion(registrationStatusConverter).IsRequired();
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
    public string HeadFirstName { get; set; } = string.Empty;
    public string HeadLastName { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
        = null;
    public DateOnly? BirthDate { get; set; }
        = null;
    public string EducationDegree { get; set; } = string.Empty;
    public string FieldOfStudy { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public bool HasTeam { get; set; }
        = true;
    public bool TeamCompleted { get; set; }
        = false;
    public string? AdditionalNotes { get; set; }
        = null;
    public RegistrationStatus Status { get; set; }
        = RegistrationStatus.Submitted;
    public DateTimeOffset? FinalizedAt { get; set; }
        = null;
    public string? SummaryFileUrl { get; set; }
        = null;
    public DateTimeOffset SubmittedAt { get; set; }
        = DateTimeOffset.UtcNow;
    public List<TeamMemberEntity> Members { get; set; } = new();
    public List<RegistrationDocumentEntity> Documents { get; set; } = new();
    public List<RegistrationLinkEntity> Links { get; set; } = new();
    public RegistrationPaymentEntity? Payment { get; set; }
        = null;
    public List<RegistrationNotificationEntity> Notifications { get; set; } = new();
    public List<BusinessPlanReviewEntity> BusinessPlanReviews { get; set; } = new();
}

public class TeamMemberEntity
{
    public Guid Id { get; set; }
    public Guid ParticipantRegistrationId { get; set; }
    public ParticipantRegistrationEntity? ParticipantRegistration { get; set; }
        = null;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FocusArea { get; set; } = string.Empty;
}

public class RegistrationDocumentEntity
{
    public Guid Id { get; set; }
    public Guid ParticipantRegistrationId { get; set; }
    public ParticipantRegistrationEntity? ParticipantRegistration { get; set; }
        = null;
    public RegistrationDocumentCategory Category { get; set; }
        = RegistrationDocumentCategory.ProjectArchive;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
}

public class RegistrationLinkEntity
{
    public Guid Id { get; set; }
    public Guid ParticipantRegistrationId { get; set; }
    public ParticipantRegistrationEntity? ParticipantRegistration { get; set; }
        = null;
    public RegistrationLinkType Type { get; set; }
        = RegistrationLinkType.Other;
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class RegistrationPaymentEntity
{
    public Guid Id { get; set; }
    public Guid ParticipantRegistrationId { get; set; }
    public ParticipantRegistrationEntity? ParticipantRegistration { get; set; }
        = null;
    public decimal Amount { get; set; }
        = 0m;
    public string Currency { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
    public RegistrationPaymentStatus Status { get; set; }
        = RegistrationPaymentStatus.Pending;
    public DateTimeOffset RequestedAt { get; set; }
        = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
        = null;
    public string? GatewayReference { get; set; }
        = null;
}

public class RegistrationNotificationEntity
{
    public Guid Id { get; set; }
    public Guid ParticipantRegistrationId { get; set; }
    public ParticipantRegistrationEntity? ParticipantRegistration { get; set; }
        = null;
    public NotificationChannel Channel { get; set; }
        = NotificationChannel.Email;
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; }
        = DateTimeOffset.UtcNow;
}

public class BusinessPlanReviewEntity
{
    public Guid Id { get; set; }
    public Guid ParticipantRegistrationId { get; set; }
    public ParticipantRegistrationEntity? ParticipantRegistration { get; set; }
        = null;
    public BusinessPlanReviewStatus Status { get; set; }
        = BusinessPlanReviewStatus.Completed;
    public decimal? OverallScore { get; set; }
        = null;
    public string Summary { get; set; } = string.Empty;
    public string Strengths { get; set; } = string.Empty;
    public string Risks { get; set; } = string.Empty;
    public string Recommendations { get; set; } = string.Empty;
    public string RawResponse { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? SourceDocumentUrl { get; set; }
        = null;
    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;
}

public class JudgeRegistrationEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
        = null;
    public DateOnly? BirthDate { get; set; }
        = null;
    public string FieldOfExpertise { get; set; } = string.Empty;
    public string HighestDegree { get; set; } = string.Empty;
    public string? Biography { get; set; }
        = null;
    public RegistrationStatus Status { get; set; }
        = RegistrationStatus.Submitted;
    public DateTimeOffset? FinalizedAt { get; set; }
        = null;
    public DateTimeOffset SubmittedAt { get; set; }
        = DateTimeOffset.UtcNow;
}

public class InvestorRegistrationEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
        = null;
    public List<string> InterestAreas { get; set; } = new();
    public string? AdditionalNotes { get; set; }
        = null;
    public RegistrationStatus Status { get; set; }
        = RegistrationStatus.Submitted;
    public DateTimeOffset? FinalizedAt { get; set; }
        = null;
    public DateTimeOffset SubmittedAt { get; set; }
        = DateTimeOffset.UtcNow;
}
