using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NabTeams.Domain.Enums;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NabTeams.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            var stringListComparer = new ValueComparer<List<string>>(
                (left, right) => (left ?? new()).SequenceEqual(right ?? new()),
                list => (list ?? new()).Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                list => (list ?? new()).ToList());

            modelBuilder.Entity("NabTeams.Infrastructure.Persistence.AppealEntity", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<RoleChannel>("Channel")
                    .HasColumnType("text")
                    .HasConversion<string>();

                b.Property<Guid>("MessageId")
                    .HasColumnType("uuid");

                b.Property<string>("Reason")
                    .IsRequired()
                    .HasMaxLength(512)
                    .HasColumnType("character varying(512)");

                b.Property<string>("ResolutionNotes")
                    .HasColumnType("text");

                b.Property<string>("ReviewedBy")
                    .HasColumnType("text");

                b.Property<DateTimeOffset?>("ReviewedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<AppealStatus>("Status")
                    .HasColumnType("text")
                    .HasConversion<string>();

                b.Property<DateTimeOffset>("SubmittedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("UserId")
                    .IsRequired()
                    .HasMaxLength(128)
                    .HasColumnType("character varying(128)");

                b.HasKey("Id");

                b.HasIndex("MessageId", "UserId")
                    .IsUnique();

                b.ToTable("Appeals");
            });

            modelBuilder.Entity("NabTeams.Infrastructure.Persistence.DisciplineEventEntity", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<int>("Delta")
                    .HasColumnType("integer");

                b.Property<Guid>("MessageId")
                    .HasColumnType("uuid");

                b.Property<DateTimeOffset>("OccurredAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("Reason")
                    .HasMaxLength(256)
                    .HasColumnType("character varying(256)");

                b.Property<Guid>("UserDisciplineId")
                    .HasColumnType("uuid");

                b.HasKey("Id");

                b.HasIndex("UserDisciplineId");

                b.ToTable("DisciplineEvents");
            });

            modelBuilder.Entity("NabTeams.Infrastructure.Persistence.InvestorRegistrationEntity", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<string>("AdditionalNotes")
                    .HasMaxLength(1024)
                    .HasColumnType("character varying(1024)");

                b.Property<string>("Email")
                    .HasMaxLength(128)
                    .HasColumnType("character varying(128)");

                b.Property<string>("FirstName")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("character varying(100)");

                b.Property<List<string>>("InterestAreas")
                    .IsRequired()
                    .HasColumnType("text")
                    .HasConversion(
                        list => string.Join('\u001F', list ?? new List<string>()),
                        value => string.IsNullOrWhiteSpace(value)
                            ? new List<string>()
                            : value.Split('\u001F', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList())
                    .Metadata.SetValueComparer(stringListComparer);

                b.Property<string>("LastName")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("character varying(100)");

                b.Property<string>("NationalId")
                    .IsRequired()
                    .HasMaxLength(16)
                    .HasColumnType("character varying(16)");

                b.Property<string>("PhoneNumber")
                    .IsRequired()
                    .HasMaxLength(32)
                    .HasColumnType("character varying(32)");

                b.Property<RegistrationStatus>("Status")
                    .HasColumnType("text")
                    .HasConversion<string>();

                b.Property<DateTimeOffset>("SubmittedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<DateTimeOffset?>("FinalizedAt")
                    .HasColumnType("timestamp with time zone");

                b.HasKey("Id");

                b.ToTable("InvestorRegistrations");
            });

            modelBuilder.Entity("NabTeams.Infrastructure.Persistence.JudgeRegistrationEntity", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<string>("Biography")
                    .HasMaxLength(1024)
                    .HasColumnType("character varying(1024)");

                b.Property<DateOnly?>("BirthDate")
                    .HasColumnType("date");

                b.Property<string>("Email")
                    .HasMaxLength(128)
                    .HasColumnType("character varying(128)");

                b.Property<string>("FieldOfExpertise")
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasColumnType("character varying(256)");

                b.Property<string>("FirstName")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("character varying(100)");

                b.Property<string>("HighestDegree")
                    .IsRequired()
                    .HasMaxLength(128)
                    .HasColumnType("character varying(128)");

                b.Property<string>("LastName")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("character varying(100)");

                b.Property<string>("NationalId")
                    .IsRequired()
                    .HasMaxLength(16)
                    .HasColumnType("character varying(16)");

                b.Property<string>("PhoneNumber")
                    .IsRequired()
                    .HasMaxLength(32)
                    .HasColumnType("character varying(32)");

                b.Property<RegistrationStatus>("Status")
                    .HasColumnType("text")
                    .HasConversion<string>();

                b.Property<DateTimeOffset>("SubmittedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<DateTimeOffset?>("FinalizedAt")
                    .HasColumnType("timestamp with time zone");

                b.HasKey("Id");

                b.ToTable("JudgeRegistrations");
            });

            modelBuilder.Entity("NabTeams.Infrastructure.Persistence.KnowledgeBaseItemEntity", b =>
            {
                b.Property<string>("Id")
                    .HasColumnType("text");

                b.Property<string>("Audience")
                    .IsRequired()
                    .HasMaxLength(32)
                    .HasColumnType("character varying(32)");

                b.Property<string>("Body")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<List<string>>("Tags")
                    .IsRequired()
                    .HasColumnType("text")
                    .HasConversion(
                        list => string.Join('\u001F', list ?? new List<string>()),
                        value => string.IsNullOrWhiteSpace(value)
                            ? new List<string>()
                            : value.Split('\u001F', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList())
                    .Metadata.SetValueComparer(stringListComparer);

                b.Property<string>("Title")
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasColumnType("character varying(256)");

                b.Property<DateTimeOffset>("UpdatedAt")
                    .HasColumnType("timestamp with time zone");

                b.HasKey("Id");

                b.ToTable("KnowledgeBaseItems");
            });

            modelBuilder.Entity("NabTeams.Infrastructure.Persistence.MessageEntity", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<RoleChannel>("Channel")
                    .HasColumnType("text")
                    .HasConversion<string>();

                b.Property<string>("Content")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<DateTimeOffset>("CreatedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("ModerationNotes")
                    .HasColumnType("text");

                b.Property<List<string>>("ModerationTags")
                    .IsRequired()
                    .HasColumnType("text")
                    .HasConversion(
                        list => string.Join('\u001F', list ?? new List<string>()),
                        value => string.IsNullOrWhiteSpace(value)
                            ? new List<string>()
                            : value.Split('\u001F', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList())
                    .Metadata.SetValueComparer(stringListComparer);

                b.Property<int>("PenaltyPoints")
                    .HasColumnType("integer");

                b.Property<string>("SenderUserId")
                    .IsRequired()
                    .HasMaxLength(128)
                    .HasColumnType("character varying(128)");

                b.Property<MessageStatus>("Status")
                    .HasColumnType("text")
                    .HasConversion<string>();

                b.HasKey("Id");

                b.ToTable("Messages");
            });

            modelBuilder.Entity("NabTeams.Infrastructure.Persistence.ModerationLogEntity", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<string>("ActionTaken")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<RoleChannel>("Channel")
                    .HasColumnType("text")
                    .HasConversion<string>();

                b.Property<DateTimeOffset>("CreatedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<Guid>("MessageId")
                    .HasColumnType("uuid");

                b.Property<List<string>>("PolicyTags")
                    .IsRequired()
                    .HasColumnType("text")
                    .HasConversion(
                        list => string.Join('\u001F', list ?? new List<string>()),
                        value => string.IsNullOrWhiteSpace(value)
                            ? new List<string>()
                            : value.Split('\u001F', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList())
                    .Metadata.SetValueComparer(stringListComparer);

                b.Property<int>("PenaltyPoints")
                    .HasColumnType("integer");

                b.Property<double>("RiskScore")
                    .HasColumnType("double precision");

                b.Property<string>("UserId")
                    .IsRequired()
                    .HasMaxLength(128)
                    .HasColumnType("character varying(128)");

                b.HasKey("Id");

                b.ToTable("ModerationLogs");
            });

            modelBuilder.Entity("NabTeams.Infrastructure.Persistence.ParticipantRegistrationEntity", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<string>("AdditionalNotes")
                    .HasMaxLength(1024)
                    .HasColumnType("character varying(1024)");

                b.Property<DateOnly?>("BirthDate")
                    .HasColumnType("date");

                b.Property<string>("EducationDegree")
                    .IsRequired()
                    .HasMaxLength(128)
                    .HasColumnType("character varying(128)");

                b.Property<string>("FieldOfStudy")
                    .IsRequired()
                    .HasMaxLength(128)
                    .HasColumnType("character varying(128)");

                b.Property<DateTimeOffset?>("FinalizedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("HeadFirstName")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("character varying(100)");

                b.Property<string>("HeadLastName")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("character varying(100)");

                b.Property<bool>("HasTeam")
                    .HasColumnType("boolean");

                b.Property<string>("NationalId")
                    .IsRequired()
                    .HasMaxLength(16)
                    .HasColumnType("character varying(16)");

                b.Property<string>("PhoneNumber")
                    .IsRequired()
                    .HasMaxLength(32)
                    .HasColumnType("character varying(32)");

                b.Property<RegistrationStatus>("Status")
                    .HasColumnType("text")
                    .HasConversion<string>();

                b.Property<DateTimeOffset>("SubmittedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("SummaryFileUrl")
                    .HasMaxLength(512)
                    .HasColumnType("character varying(512)");

                b.Property<string>("TeamName")
                    .IsRequired()
                    .HasMaxLength(128)
                    .HasColumnType("character varying(128)");

                b.Property<bool>("TeamCompleted")
                    .HasColumnType("boolean");

                b.Property<string>("Email")
                    .HasMaxLength(128)
                    .HasColumnType("character varying(128)");

                b.HasKey("Id");

                b.ToTable("ParticipantRegistrations");
            });

            modelBuilder.Entity("NabTeams.Infrastructure.Persistence.RegistrationDocumentEntity", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<RegistrationDocumentCategory>("Category")
                    .HasColumnType("text")
                    .HasConversion<string>();

                b.Property<string>("FileName")
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasColumnType("character varying(256)");

                b.Property<string>("FileUrl")
                    .IsRequired()
                    .HasMaxLength(512)
                    .HasColumnType("character varying(512)");

                b.Property<Guid>("ParticipantRegistrationId")
                    .HasColumnType("uuid");

                b.HasKey("Id");

                b.HasIndex("ParticipantRegistrationId");

                b.ToTable("RegistrationDocuments");
            });

            modelBuilder.Entity("NabTeams.Infrastructure.Persistence.RegistrationLinkEntity", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<string>("Label")
                    .IsRequired()
                    .HasMaxLength(128)
                    .HasColumnType("character varying(128)");

                b.Property<Guid>("ParticipantRegistrationId")
                    .HasColumnType("uuid");

                b.Property<RegistrationLinkType>("Type")
                    .HasColumnType("text")
                    .HasConversion<string>();

                b.Property<string>("Url")
                    .IsRequired()
                    .HasMaxLength(512)
                    .HasColumnType("character varying(512)");

                b.HasKey("Id");

                b.HasIndex("ParticipantRegistrationId");

                b.ToTable("RegistrationLinks");
            });

            modelBuilder.Entity("NabTeams.Infrastructure.Persistence.TeamMemberEntity", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<string>("FocusArea")
                    .IsRequired()
                    .HasMaxLength(150)
                    .HasColumnType("character varying(150)");

                b.Property<string>("FullName")
                    .IsRequired()
                    .HasMaxLength(150)
                    .HasColumnType("character varying(150)");

                b.Property<Guid>("ParticipantRegistrationId")
                    .HasColumnType("uuid");

                b.Property<string>("Role")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("character varying(100)");

                b.HasKey("Id");

                b.HasIndex("ParticipantRegistrationId");

                b.ToTable("TeamMembers");
            });

            modelBuilder.Entity("NabTeams.Infrastructure.Persistence.UserDisciplineEntity", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<RoleChannel>("Channel")
                    .HasColumnType("text")
                    .HasConversion<string>();

                b.Property<int>("ScoreBalance")
                    .HasColumnType("integer");

                b.Property<string>("UserId")
                    .IsRequired()
                    .HasMaxLength(128)
                    .HasColumnType("character varying(128)");

                b.HasKey("Id");

                b.HasIndex("UserId", "Channel")
                    .IsUnique();

                b.ToTable("UserDisciplines");
            });

            modelBuilder.Entity("NabTeams.Infrastructure.Persistence.DisciplineEventEntity", b =>
            {
                b.HasOne("NabTeams.Infrastructure.Persistence.UserDisciplineEntity", "UserDiscipline")
                    .WithMany("Events")
                    .HasForeignKey("UserDisciplineId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("UserDiscipline");
            });

            modelBuilder.Entity("NabTeams.Infrastructure.Persistence.RegistrationDocumentEntity", b =>
            {
                b.HasOne("NabTeams.Infrastructure.Persistence.ParticipantRegistrationEntity", "ParticipantRegistration")
                    .WithMany("Documents")
                    .HasForeignKey("ParticipantRegistrationId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("ParticipantRegistration");
            });

            modelBuilder.Entity("NabTeams.Infrastructure.Persistence.RegistrationLinkEntity", b =>
            {
                b.HasOne("NabTeams.Infrastructure.Persistence.ParticipantRegistrationEntity", "ParticipantRegistration")
                    .WithMany("Links")
                    .HasForeignKey("ParticipantRegistrationId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("ParticipantRegistration");
            });

            modelBuilder.Entity("NabTeams.Infrastructure.Persistence.TeamMemberEntity", b =>
            {
                b.HasOne("NabTeams.Infrastructure.Persistence.ParticipantRegistrationEntity", "ParticipantRegistration")
                    .WithMany("Members")
                    .HasForeignKey("ParticipantRegistrationId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("ParticipantRegistration");
            });

            modelBuilder.Entity("NabTeams.Infrastructure.Persistence.ParticipantRegistrationEntity", b =>
            {
                b.Navigation("Documents");

                b.Navigation("Links");

                b.Navigation("Members");
            });

            modelBuilder.Entity("NabTeams.Infrastructure.Persistence.UserDisciplineEntity", b =>
            {
                b.Navigation("Events");
            });
        }
    }
}
