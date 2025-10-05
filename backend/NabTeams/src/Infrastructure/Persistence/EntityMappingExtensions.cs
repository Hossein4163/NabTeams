using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using System.Collections.Generic;
using System.Linq;

namespace NabTeams.Infrastructure.Persistence;

public static class EntityMappingExtensions
{
    public static Message ToModel(this MessageEntity entity)
        => new()
        {
            Id = entity.Id,
            Channel = entity.Channel,
            SenderUserId = entity.SenderUserId,
            Content = entity.Content,
            CreatedAt = entity.CreatedAt,
            Status = entity.Status,
            ModerationRisk = entity.ModerationRisk,
            ModerationTags = entity.ModerationTags.ToList(),
            ModerationNotes = entity.ModerationNotes,
            PenaltyPoints = entity.PenaltyPoints
        };

    public static MessageEntity ToEntity(this Message model)
        => new()
        {
            Id = model.Id,
            Channel = model.Channel,
            SenderUserId = model.SenderUserId,
            Content = model.Content,
            CreatedAt = model.CreatedAt,
            Status = model.Status,
            ModerationRisk = model.ModerationRisk,
            ModerationTags = model.ModerationTags.ToList(),
            ModerationNotes = model.ModerationNotes,
            PenaltyPoints = model.PenaltyPoints
        };

    public static ModerationLog ToModel(this ModerationLogEntity entity)
        => new()
        {
            Id = entity.Id,
            MessageId = entity.MessageId,
            UserId = entity.UserId,
            Channel = entity.Channel,
            RiskScore = entity.RiskScore,
            PolicyTags = entity.PolicyTags.ToList(),
            ActionTaken = entity.ActionTaken,
            PenaltyPoints = entity.PenaltyPoints,
            CreatedAt = entity.CreatedAt
        };

    public static ModerationLogEntity ToEntity(this ModerationLog model)
        => new()
        {
            Id = model.Id,
            MessageId = model.MessageId,
            UserId = model.UserId,
            Channel = model.Channel,
            RiskScore = model.RiskScore,
            PolicyTags = model.PolicyTags.ToList(),
            ActionTaken = model.ActionTaken,
            PenaltyPoints = model.PenaltyPoints,
            CreatedAt = model.CreatedAt
        };

    public static UserDiscipline ToModel(this UserDisciplineEntity entity)
    {
        var record = new UserDiscipline
        {
            UserId = entity.UserId,
            Channel = entity.Channel,
            ScoreBalance = entity.ScoreBalance
        };

        var orderedEvents = entity.Events
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => new DisciplineEvent
            {
                MessageId = e.MessageId,
                Delta = e.Delta,
                OccurredAt = e.OccurredAt,
                Reason = e.Reason
            });

        record.History.AddRange(orderedEvents);
        return record;
    }

    public static KnowledgeBaseItem ToModel(this KnowledgeBaseItemEntity entity)
        => new()
        {
            Id = entity.Id,
            Title = entity.Title,
            Body = entity.Body,
            Audience = entity.Audience,
            Tags = entity.Tags.ToList(),
            UpdatedAt = entity.UpdatedAt
        };

    public static KnowledgeBaseItemEntity ToEntity(this KnowledgeBaseItem model)
        => new()
        {
            Id = model.Id,
            Title = model.Title,
            Body = model.Body,
            Audience = model.Audience,
            Tags = model.Tags.ToList(),
            UpdatedAt = model.UpdatedAt
        };

    public static Appeal ToModel(this AppealEntity entity)
        => new()
        {
            Id = entity.Id,
            MessageId = entity.MessageId,
            Channel = entity.Channel,
            UserId = entity.UserId,
            SubmittedAt = entity.SubmittedAt,
            Reason = entity.Reason,
            Status = entity.Status,
            ResolutionNotes = entity.ResolutionNotes,
            ReviewedBy = entity.ReviewedBy,
            ReviewedAt = entity.ReviewedAt
        };

    public static AppealEntity ToEntity(this Appeal model)
        => new()
        {
            Id = model.Id,
            MessageId = model.MessageId,
            Channel = model.Channel,
            UserId = model.UserId,
            SubmittedAt = model.SubmittedAt,
            Reason = model.Reason,
            Status = model.Status,
            ResolutionNotes = model.ResolutionNotes,
            ReviewedBy = model.ReviewedBy,
            ReviewedAt = model.ReviewedAt
        };

    public static ParticipantRegistration ToModel(this ParticipantRegistrationEntity entity)
        => new()
        {
            Id = entity.Id,
            HeadFirstName = entity.HeadFirstName,
            HeadLastName = entity.HeadLastName,
            NationalId = entity.NationalId,
            PhoneNumber = entity.PhoneNumber,
            Email = entity.Email,
            BirthDate = entity.BirthDate,
            EducationDegree = entity.EducationDegree,
            FieldOfStudy = entity.FieldOfStudy,
            TeamName = entity.TeamName,
            HasTeam = entity.HasTeam,
            TeamCompleted = entity.TeamCompleted,
            AdditionalNotes = entity.AdditionalNotes,
            Status = entity.Status,
            FinalizedAt = entity.FinalizedAt,
            SummaryFileUrl = entity.SummaryFileUrl,
            SubmittedAt = entity.SubmittedAt,
            Members = (entity.Members ?? new List<TeamMemberEntity>())
                .OrderBy(m => m.FullName)
                .Select(m => new TeamMember
                {
                    Id = m.Id,
                    FullName = m.FullName,
                    Role = m.Role,
                    FocusArea = m.FocusArea
                })
                .ToList(),
            Documents = (entity.Documents ?? new List<RegistrationDocumentEntity>())
                .Select(d => new RegistrationDocument
                {
                    Id = d.Id,
                    Category = d.Category,
                    FileName = d.FileName,
                    FileUrl = d.FileUrl
                })
                .ToList(),
            Links = (entity.Links ?? new List<RegistrationLinkEntity>())
                .Select(l => new RegistrationLink
                {
                    Id = l.Id,
                    Type = l.Type,
                    Label = l.Label,
                    Url = l.Url
                })
                .ToList(),
            Payment = entity.Payment?.ToModel(),
            Notifications = (entity.Notifications ?? new List<RegistrationNotificationEntity>())
                .OrderByDescending(n => n.SentAt)
                .Select(n => n.ToModel())
                .ToList()
        };

    public static ParticipantRegistrationEntity ToEntity(this ParticipantRegistration model)
    {
        var entity = new ParticipantRegistrationEntity
        {
            Id = model.Id,
            HeadFirstName = model.HeadFirstName,
            HeadLastName = model.HeadLastName,
            NationalId = model.NationalId,
            PhoneNumber = model.PhoneNumber,
            Email = model.Email,
            BirthDate = model.BirthDate,
            EducationDegree = model.EducationDegree,
            FieldOfStudy = model.FieldOfStudy,
            TeamName = model.TeamName,
            HasTeam = model.HasTeam,
            TeamCompleted = model.TeamCompleted,
            AdditionalNotes = model.AdditionalNotes,
            Status = model.Status,
            FinalizedAt = model.FinalizedAt,
            SummaryFileUrl = model.SummaryFileUrl,
            SubmittedAt = model.SubmittedAt
        };

        entity.UpdateCollections(model);
        if (model.Payment is not null)
        {
            entity.Payment = model.Payment.ToEntity();
            entity.Payment.ParticipantRegistrationId = entity.Id;
        }

        entity.Notifications = model.Notifications
            .Select(n =>
            {
                var notificationEntity = n.ToEntity();
                notificationEntity.ParticipantRegistrationId = entity.Id;
                return notificationEntity;
            })
            .ToList();

        return entity;
    }

    public static void UpdateCollections(this ParticipantRegistrationEntity entity, ParticipantRegistration model)
    {
        entity.Members = model.Members
            .Select(m => new TeamMemberEntity
            {
                Id = m.Id,
                ParticipantRegistrationId = entity.Id,
                FullName = m.FullName,
                Role = m.Role,
                FocusArea = m.FocusArea
            })
            .ToList();

        entity.Documents = model.Documents
            .Select(d => new RegistrationDocumentEntity
            {
                Id = d.Id,
                ParticipantRegistrationId = entity.Id,
                Category = d.Category,
                FileName = d.FileName,
                FileUrl = d.FileUrl
            })
            .ToList();

        entity.Links = model.Links
            .Select(l => new RegistrationLinkEntity
            {
                Id = l.Id,
                ParticipantRegistrationId = entity.Id,
                Type = l.Type,
                Label = l.Label,
                Url = l.Url
            })
            .ToList();

        if (model.Payment is not null)
        {
            entity.Payment = model.Payment.ToEntity();
            entity.Payment.ParticipantRegistrationId = entity.Id;
        }

        entity.Notifications = model.Notifications
            .Select(n =>
            {
                var notificationEntity = n.ToEntity();
                notificationEntity.ParticipantRegistrationId = entity.Id;
                return notificationEntity;
            })
            .ToList();
    }

    public static RegistrationPayment ToModel(this RegistrationPaymentEntity entity)
        => new()
        {
            Id = entity.Id,
            ParticipantRegistrationId = entity.ParticipantRegistrationId,
            Amount = entity.Amount,
            Currency = entity.Currency,
            PaymentUrl = entity.PaymentUrl,
            Status = entity.Status,
            RequestedAt = entity.RequestedAt,
            CompletedAt = entity.CompletedAt,
            GatewayReference = entity.GatewayReference
        };

    public static RegistrationPaymentEntity ToEntity(this RegistrationPayment model)
        => new()
        {
            Id = model.Id,
            ParticipantRegistrationId = model.ParticipantRegistrationId,
            Amount = model.Amount,
            Currency = model.Currency,
            PaymentUrl = model.PaymentUrl,
            Status = model.Status,
            RequestedAt = model.RequestedAt,
            CompletedAt = model.CompletedAt,
            GatewayReference = model.GatewayReference
        };

    public static RegistrationNotification ToModel(this RegistrationNotificationEntity entity)
        => new()
        {
            Id = entity.Id,
            ParticipantRegistrationId = entity.ParticipantRegistrationId,
            Channel = entity.Channel,
            Recipient = entity.Recipient,
            Subject = entity.Subject,
            Message = entity.Message,
            SentAt = entity.SentAt
        };

    public static RegistrationNotificationEntity ToEntity(this RegistrationNotification model)
        => new()
        {
            Id = model.Id,
            ParticipantRegistrationId = model.ParticipantRegistrationId,
            Channel = model.Channel,
            Recipient = model.Recipient,
            Subject = model.Subject,
            Message = model.Message,
            SentAt = model.SentAt
        };

    public static JudgeRegistration ToModel(this JudgeRegistrationEntity entity)
            => new()
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            NationalId = entity.NationalId,
            PhoneNumber = entity.PhoneNumber,
            Email = entity.Email,
            BirthDate = entity.BirthDate,
            FieldOfExpertise = entity.FieldOfExpertise,
            HighestDegree = entity.HighestDegree,
            Biography = entity.Biography,
            Status = entity.Status,
            FinalizedAt = entity.FinalizedAt,
            SubmittedAt = entity.SubmittedAt
        };

    public static JudgeRegistrationEntity ToEntity(this JudgeRegistration model)
        => new()
        {
            Id = model.Id,
            FirstName = model.FirstName,
            LastName = model.LastName,
            NationalId = model.NationalId,
            PhoneNumber = model.PhoneNumber,
            Email = model.Email,
            BirthDate = model.BirthDate,
            FieldOfExpertise = model.FieldOfExpertise,
            HighestDegree = model.HighestDegree,
            Biography = model.Biography,
            Status = model.Status,
            FinalizedAt = model.FinalizedAt,
            SubmittedAt = model.SubmittedAt
        };

    public static InvestorRegistration ToModel(this InvestorRegistrationEntity entity)
        => new()
        {
            Id = entity.Id,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            NationalId = entity.NationalId,
            PhoneNumber = entity.PhoneNumber,
            Email = entity.Email,
            InterestAreas = entity.InterestAreas.ToList(),
            AdditionalNotes = entity.AdditionalNotes,
            Status = entity.Status,
            FinalizedAt = entity.FinalizedAt,
            SubmittedAt = entity.SubmittedAt
        };

    public static InvestorRegistrationEntity ToEntity(this InvestorRegistration model)
        => new()
        {
            Id = model.Id,
            FirstName = model.FirstName,
            LastName = model.LastName,
            NationalId = model.NationalId,
            PhoneNumber = model.PhoneNumber,
            Email = model.Email,
            InterestAreas = model.InterestAreas.ToList(),
            AdditionalNotes = model.AdditionalNotes,
            Status = model.Status,
            FinalizedAt = model.FinalizedAt,
            SubmittedAt = model.SubmittedAt
        };
}
