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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var channelConverter = new EnumToStringConverter<RoleChannel>();
        var statusConverter = new EnumToStringConverter<MessageStatus>();
        var appealStatusConverter = new EnumToStringConverter<AppealStatus>();

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
