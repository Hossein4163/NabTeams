using NabTeams.Domain.Entities;

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
    {
        var model = new ParticipantRegistration
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Status = entity.Status,
            Stage = entity.Stage,
            CreatedAt = entity.CreatedAt,
            SubmittedAt = entity.SubmittedAt,
            ApprovedAt = entity.ApprovedAt,
            HeadFullName = entity.HeadFullName,
            HeadNationalId = entity.HeadNationalId,
            HeadPhoneNumber = entity.HeadPhoneNumber,
            HeadBirthDate = entity.HeadBirthDate,
            HeadDegree = entity.HeadDegree,
            HeadMajor = entity.HeadMajor,
            TeamName = entity.TeamName,
            HasTeam = entity.HasTeam,
            TeamCompleted = entity.TeamCompleted,
            ProjectFileUrl = entity.ProjectFileUrl,
            ResumeFileUrl = entity.ResumeFileUrl,
            SocialLinks = entity.SocialLinks.ToList(),
            FinalSummary = entity.FinalSummary,
            JudgeNotes = entity.JudgeNotes,
            WorkspaceId = entity.WorkspaceId
        };

        foreach (var member in entity.TeamMembers.OrderBy(m => m.FullName))
        {
            model.TeamMembers.Add(new TeamMember
            {
                Id = member.Id,
                FullName = member.FullName,
                Role = member.Role,
                FocusArea = member.FocusArea
            });
        }

        return model;
    }

    public static ParticipantRegistrationEntity ToEntity(this ParticipantRegistration model)
    {
        var entity = new ParticipantRegistrationEntity
        {
            Id = model.Id,
            UserId = model.UserId,
            Status = model.Status,
            Stage = model.Stage,
            CreatedAt = model.CreatedAt,
            SubmittedAt = model.SubmittedAt,
            ApprovedAt = model.ApprovedAt,
            HeadFullName = model.HeadFullName,
            HeadNationalId = model.HeadNationalId,
            HeadPhoneNumber = model.HeadPhoneNumber,
            HeadBirthDate = model.HeadBirthDate,
            HeadDegree = model.HeadDegree,
            HeadMajor = model.HeadMajor,
            TeamName = model.TeamName,
            HasTeam = model.HasTeam,
            TeamCompleted = model.TeamCompleted,
            ProjectFileUrl = model.ProjectFileUrl,
            ResumeFileUrl = model.ResumeFileUrl,
            SocialLinks = model.SocialLinks.ToList(),
            FinalSummary = model.FinalSummary,
            JudgeNotes = model.JudgeNotes,
            WorkspaceId = model.WorkspaceId
        };

        entity.TeamMembers = model.TeamMembers.Select(member => new TeamMemberEntity
        {
            Id = member.Id,
            ParticipantId = entity.Id,
            FullName = member.FullName,
            Role = member.Role,
            FocusArea = member.FocusArea
        }).ToList();

        return entity;
    }

    public static JudgeRegistration ToModel(this JudgeRegistrationEntity entity)
        => new()
        {
            Id = entity.Id,
            UserId = entity.UserId,
            FullName = entity.FullName,
            NationalId = entity.NationalId,
            PhoneNumber = entity.PhoneNumber,
            BirthDate = entity.BirthDate,
            ExpertiseArea = entity.ExpertiseArea,
            HighestDegree = entity.HighestDegree,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            SubmittedAt = entity.SubmittedAt,
            ApprovedAt = entity.ApprovedAt,
            Notes = entity.Notes
        };

    public static JudgeRegistrationEntity ToEntity(this JudgeRegistration model)
        => new()
        {
            Id = model.Id,
            UserId = model.UserId,
            FullName = model.FullName,
            NationalId = model.NationalId,
            PhoneNumber = model.PhoneNumber,
            BirthDate = model.BirthDate,
            ExpertiseArea = model.ExpertiseArea,
            HighestDegree = model.HighestDegree,
            Status = model.Status,
            CreatedAt = model.CreatedAt,
            SubmittedAt = model.SubmittedAt,
            ApprovedAt = model.ApprovedAt,
            Notes = model.Notes
        };

    public static InvestorRegistration ToModel(this InvestorRegistrationEntity entity)
        => new()
        {
            Id = entity.Id,
            UserId = entity.UserId,
            FullName = entity.FullName,
            NationalId = entity.NationalId,
            PhoneNumber = entity.PhoneNumber,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            SubmittedAt = entity.SubmittedAt,
            ApprovedAt = entity.ApprovedAt,
            InterestAreas = entity.InterestAreas.ToList(),
            Notes = entity.Notes
        };

    public static InvestorRegistrationEntity ToEntity(this InvestorRegistration model)
        => new()
        {
            Id = model.Id,
            UserId = model.UserId,
            FullName = model.FullName,
            NationalId = model.NationalId,
            PhoneNumber = model.PhoneNumber,
            Status = model.Status,
            CreatedAt = model.CreatedAt,
            SubmittedAt = model.SubmittedAt,
            ApprovedAt = model.ApprovedAt,
            InterestAreas = model.InterestAreas.ToList(),
            Notes = model.Notes
        };

    public static ProjectWorkspace ToModel(this ProjectWorkspaceEntity entity)
    {
        var workspace = new ProjectWorkspace
        {
            Id = entity.Id,
            ParticipantRegistrationId = entity.ParticipantRegistrationId,
            ProjectName = entity.ProjectName,
            Vision = entity.Vision,
            BusinessModelSummary = entity.BusinessModelSummary,
            CreatedAt = entity.CreatedAt
        };

        foreach (var task in entity.Tasks.OrderBy(t => t.CreatedAt))
        {
            workspace.Tasks.Add(new ProjectTask
            {
                Id = task.Id,
                WorkspaceId = task.WorkspaceId,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                Assignee = task.Assignee,
                Status = task.Status,
                CreatedAt = task.CreatedAt,
                CompletedAt = task.CompletedAt
            });
        }

        foreach (var request in entity.StaffingRequests.OrderByDescending(r => r.CreatedAt))
        {
            workspace.StaffingRequests.Add(new StaffingRequest
            {
                Id = request.Id,
                WorkspaceId = request.WorkspaceId,
                Skill = request.Skill,
                Description = request.Description,
                IsPaidOpportunity = request.IsPaidOpportunity,
                Status = request.Status,
                CreatedAt = request.CreatedAt
            });
        }

        foreach (var insight in entity.Insights.OrderByDescending(i => i.CreatedAt))
        {
            workspace.Insights.Add(new AiInsight
            {
                Id = insight.Id,
                WorkspaceId = insight.WorkspaceId,
                Type = insight.Type,
                Summary = insight.Summary,
                ImprovementAreas = insight.ImprovementAreas,
                Confidence = insight.Confidence,
                CreatedAt = insight.CreatedAt
            });
        }

        return workspace;
    }

    public static ProjectWorkspaceEntity ToEntity(this ProjectWorkspace model)
    {
        var entity = new ProjectWorkspaceEntity
        {
            Id = model.Id,
            ParticipantRegistrationId = model.ParticipantRegistrationId,
            ProjectName = model.ProjectName,
            Vision = model.Vision,
            BusinessModelSummary = model.BusinessModelSummary,
            CreatedAt = model.CreatedAt
        };

        entity.Tasks = model.Tasks.Select(task => new ProjectTaskEntity
        {
            Id = task.Id,
            WorkspaceId = entity.Id,
            Title = task.Title,
            Description = task.Description,
            DueDate = task.DueDate,
            Assignee = task.Assignee,
            Status = task.Status,
            CreatedAt = task.CreatedAt,
            CompletedAt = task.CompletedAt
        }).ToList();

        entity.StaffingRequests = model.StaffingRequests.Select(request => new StaffingRequestEntity
        {
            Id = request.Id,
            WorkspaceId = entity.Id,
            Skill = request.Skill,
            Description = request.Description,
            IsPaidOpportunity = request.IsPaidOpportunity,
            Status = request.Status,
            CreatedAt = request.CreatedAt
        }).ToList();

        entity.Insights = model.Insights.Select(insight => new AiInsightEntity
        {
            Id = insight.Id,
            WorkspaceId = entity.Id,
            Type = insight.Type,
            Summary = insight.Summary,
            ImprovementAreas = insight.ImprovementAreas,
            Confidence = insight.Confidence,
            CreatedAt = insight.CreatedAt
        }).ToList();

        return entity;
    }

    public static PaymentRecord ToModel(this PaymentRecordEntity entity)
        => new()
        {
            Id = entity.Id,
            RegistrationId = entity.RegistrationId,
            RegistrationType = entity.RegistrationType,
            Amount = entity.Amount,
            Status = entity.Status,
            GatewayUrl = entity.GatewayUrl,
            ReferenceCode = entity.ReferenceCode,
            CreatedAt = entity.CreatedAt,
            PaidAt = entity.PaidAt,
            Notes = entity.Notes
        };

    public static PaymentRecordEntity ToEntity(this PaymentRecord model)
        => new()
        {
            Id = model.Id,
            RegistrationId = model.RegistrationId,
            RegistrationType = model.RegistrationType,
            Amount = model.Amount,
            Status = model.Status,
            GatewayUrl = model.GatewayUrl,
            ReferenceCode = model.ReferenceCode,
            CreatedAt = model.CreatedAt,
            PaidAt = model.PaidAt,
            Notes = model.Notes
        };

    public static NotificationLog ToModel(this NotificationLogEntity entity)
        => new()
        {
            Id = entity.Id,
            RegistrationId = entity.RegistrationId,
            RegistrationType = entity.RegistrationType,
            Channel = entity.Channel,
            Message = entity.Message,
            SentAt = entity.SentAt
        };

    public static NotificationLogEntity ToEntity(this NotificationLog model)
        => new()
        {
            Id = model.Id,
            RegistrationId = model.RegistrationId,
            RegistrationType = model.RegistrationType,
            Channel = model.Channel,
            Message = model.Message,
            SentAt = model.SentAt
        };
}
