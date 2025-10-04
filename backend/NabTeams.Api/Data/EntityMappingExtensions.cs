using NabTeams.Api.Models;

namespace NabTeams.Api.Data;

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
}
