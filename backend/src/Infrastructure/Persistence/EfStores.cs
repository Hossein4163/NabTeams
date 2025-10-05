using Microsoft.EntityFrameworkCore;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Infrastructure.Persistence;

public class EfChatRepository : IChatRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EfChatRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddMessageAsync(Message message, CancellationToken cancellationToken = default)
    {
        var entity = message.ToEntity();
        _dbContext.Messages.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Message>> GetMessagesAsync(RoleChannel channel, CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.Messages
            .AsNoTracking()
            .Where(m => m.Channel == channel && m.Status != MessageStatus.Blocked)
            .OrderBy(m => m.CreatedAt)
            .Take(500)
            .ToListAsync(cancellationToken);

        return items.Select(m => m.ToModel()).ToList();
    }

    public async Task<Message?> GetMessageAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Messages
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == id, cancellationToken);

        return entity?.ToModel();
    }

    public async Task UpdateMessageModerationAsync(
        Guid messageId,
        MessageStatus status,
        double risk,
        IReadOnlyCollection<string> tags,
        string? notes,
        int penaltyPoints,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Messages
            .SingleOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (entity is null)
        {
            return;
        }

        entity.Status = status;
        entity.ModerationRisk = risk;
        entity.ModerationTags = tags.ToList();
        entity.ModerationNotes = notes;
        entity.PenaltyPoints = penaltyPoints;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class EfModerationLogStore : IModerationLogStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfModerationLogStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ModerationLog log, CancellationToken cancellationToken = default)
    {
        _dbContext.ModerationLogs.Add(log.ToEntity());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ModerationLog>> QueryAsync(RoleChannel channel, CancellationToken cancellationToken = default)
    {
        var logs = await _dbContext.ModerationLogs
            .AsNoTracking()
            .Where(l => l.Channel == channel)
            .OrderByDescending(l => l.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return logs.Select(l => l.ToModel()).ToList();
    }

    public async Task<ModerationLog?> GetAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ModerationLogs
            .AsNoTracking()
            .SingleOrDefaultAsync(l => l.MessageId == messageId, cancellationToken);

        return entity?.ToModel();
    }
}

public class EfUserDisciplineStore : IUserDisciplineStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfUserDisciplineStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserDiscipline> UpdateScoreAsync(string userId, RoleChannel channel, int delta, string reason, Guid messageId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.UserDisciplines
            .Include(x => x.Events)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Channel == channel, cancellationToken);

        if (record is null)
        {
            record = new UserDisciplineEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Channel = channel,
                ScoreBalance = 0
            };
            _dbContext.UserDisciplines.Add(record);
        }

        record.ScoreBalance += delta;
        record.Events.Add(new DisciplineEventEntity
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            OccurredAt = DateTimeOffset.UtcNow,
            Delta = delta,
            Reason = reason,
            UserDisciplineId = record.Id
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetAsync(userId, channel, cancellationToken))!;
    }

    public async Task<UserDiscipline?> GetAsync(string userId, RoleChannel channel, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.UserDisciplines
            .AsNoTracking()
            .Include(x => x.Events)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.Channel == channel, cancellationToken);

        return record?.ToModel();
    }
}

public class EfSupportKnowledgeBase : ISupportKnowledgeBase
{
    private readonly ApplicationDbContext _dbContext;

    public EfSupportKnowledgeBase(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<KnowledgeBaseItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.KnowledgeBaseItems
            .AsNoTracking()
            .OrderByDescending(i => i.UpdatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(i => i.ToModel()).ToList();
    }

    public async Task<KnowledgeBaseItem> UpsertAsync(KnowledgeBaseItem item, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.KnowledgeBaseItems
            .SingleOrDefaultAsync(i => i.Id == item.Id, cancellationToken);

        if (existing is null)
        {
            existing = item.ToEntity();
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            _dbContext.KnowledgeBaseItems.Add(existing);
        }
        else
        {
            existing.Title = item.Title;
            existing.Body = item.Body;
            existing.Audience = item.Audience;
            existing.Tags = item.Tags.ToList();
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing.ToModel();
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.KnowledgeBaseItems
            .SingleOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (existing is null)
        {
            return;
        }

        _dbContext.KnowledgeBaseItems.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class EfAppealStore : IAppealStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfAppealStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Appeal> CreateAsync(Appeal appeal, CancellationToken cancellationToken = default)
    {
        var entity = appeal.ToEntity();
        _dbContext.Appeals.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    public async Task<IReadOnlyCollection<Appeal>> GetForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.Appeals
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync(cancellationToken);

        return items.Select(a => a.ToModel()).ToList();
    }

    public async Task<IReadOnlyCollection<Appeal>> QueryAsync(RoleChannel? channel, AppealStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Appeals.AsQueryable();

        if (channel.HasValue)
        {
            query = query.Where(a => a.Channel == channel.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        var items = await query
            .AsNoTracking()
            .OrderByDescending(a => a.SubmittedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return items.Select(a => a.ToModel()).ToList();
    }

    public async Task<Appeal?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Appeals
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

        return entity?.ToModel();
    }

    public async Task<Appeal?> ResolveAsync(Guid id, AppealStatus status, string reviewerId, string? notes, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Appeals
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Status = status;
        entity.ReviewedBy = reviewerId;
        entity.ReviewedAt = DateTimeOffset.UtcNow;
        entity.ResolutionNotes = notes;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

public class EfParticipantRegistrationStore : IParticipantRegistrationStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfParticipantRegistrationStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ParticipantRegistration?> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ParticipantRegistrations
            .AsNoTracking()
            .Include(p => p.TeamMembers)
            .SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        return entity?.ToModel();
    }

    public async Task<ParticipantRegistration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ParticipantRegistrations
            .AsNoTracking()
            .Include(p => p.TeamMembers)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

        return entity?.ToModel();
    }

    public async Task<IReadOnlyCollection<ParticipantRegistration>> ListAsync(RegistrationStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ParticipantRegistrations
            .AsNoTracking()
            .Include(p => p.TeamMembers)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return items.Select(p => p.ToModel()).ToList();
    }

    public async Task<ParticipantRegistration> SaveAsync(ParticipantRegistration registration, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ParticipantRegistrations
            .Include(p => p.TeamMembers)
            .SingleOrDefaultAsync(p => p.Id == registration.Id, cancellationToken);

        if (existing is null)
        {
            var entity = registration.ToEntity();
            _dbContext.ParticipantRegistrations.Add(entity);
        }
        else
        {
            existing.UserId = registration.UserId;
            existing.Status = registration.Status;
            existing.Stage = registration.Stage;
            existing.CreatedAt = registration.CreatedAt;
            existing.SubmittedAt = registration.SubmittedAt;
            existing.ApprovedAt = registration.ApprovedAt;
            existing.HeadFullName = registration.HeadFullName;
            existing.HeadNationalId = registration.HeadNationalId;
            existing.HeadPhoneNumber = registration.HeadPhoneNumber;
            existing.HeadBirthDate = registration.HeadBirthDate;
            existing.HeadDegree = registration.HeadDegree;
            existing.HeadMajor = registration.HeadMajor;
            existing.TeamName = registration.TeamName;
            existing.HasTeam = registration.HasTeam;
            existing.TeamCompleted = registration.TeamCompleted;
            existing.ProjectFileUrl = registration.ProjectFileUrl;
            existing.ResumeFileUrl = registration.ResumeFileUrl;
            existing.FinalSummary = registration.FinalSummary;
            existing.JudgeNotes = registration.JudgeNotes;
            existing.WorkspaceId = registration.WorkspaceId;
            existing.SocialLinks = registration.SocialLinks.ToList();

            _dbContext.TeamMembers.RemoveRange(existing.TeamMembers);
            existing.TeamMembers = registration.TeamMembers.Select(member => new TeamMemberEntity
            {
                Id = member.Id,
                ParticipantId = existing.Id,
                FullName = member.FullName,
                Role = member.Role,
                FocusArea = member.FocusArea
            }).ToList();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var updated = await _dbContext.ParticipantRegistrations
            .AsNoTracking()
            .Include(p => p.TeamMembers)
            .SingleAsync(p => p.Id == registration.Id, cancellationToken);

        return updated.ToModel();
    }

    public async Task UpdateStatusAsync(Guid id, RegistrationStatus status, string? judgeNotes, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ParticipantRegistrations
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (entity is null)
        {
            return;
        }

        entity.Status = status;
        entity.JudgeNotes = judgeNotes;

        if (status == RegistrationStatus.Submitted || status == RegistrationStatus.UnderReview)
        {
            entity.Stage = ParticipantRegistrationStage.Completed;
            entity.SubmittedAt ??= DateTimeOffset.UtcNow;
        }

        if (status == RegistrationStatus.Approved)
        {
            entity.ApprovedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class EfJudgeRegistrationStore : IJudgeRegistrationStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfJudgeRegistrationStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<JudgeRegistration?> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.JudgeRegistrations
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.UserId == userId, cancellationToken);

        return entity?.ToModel();
    }

    public async Task<IReadOnlyCollection<JudgeRegistration>> ListAsync(RegistrationStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.JudgeRegistrations.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return items.Select(r => r.ToModel()).ToList();
    }

    public async Task<JudgeRegistration> SaveAsync(JudgeRegistration registration, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.JudgeRegistrations
            .SingleOrDefaultAsync(r => r.Id == registration.Id, cancellationToken);

        if (entity is null)
        {
            _dbContext.JudgeRegistrations.Add(registration.ToEntity());
        }
        else
        {
            entity.UserId = registration.UserId;
            entity.FullName = registration.FullName;
            entity.NationalId = registration.NationalId;
            entity.PhoneNumber = registration.PhoneNumber;
            entity.BirthDate = registration.BirthDate;
            entity.ExpertiseArea = registration.ExpertiseArea;
            entity.HighestDegree = registration.HighestDegree;
            entity.Status = registration.Status;
            entity.CreatedAt = registration.CreatedAt;
            entity.SubmittedAt = registration.SubmittedAt;
            entity.ApprovedAt = registration.ApprovedAt;
            entity.Notes = registration.Notes;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var updated = await _dbContext.JudgeRegistrations
            .AsNoTracking()
            .SingleAsync(r => r.Id == registration.Id, cancellationToken);

        return updated.ToModel();
    }

    public async Task UpdateStatusAsync(Guid id, RegistrationStatus status, string? notes, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.JudgeRegistrations
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (entity is null)
        {
            return;
        }

        entity.Status = status;
        entity.Notes = notes;

        if (status == RegistrationStatus.Submitted || status == RegistrationStatus.UnderReview)
        {
            entity.SubmittedAt ??= DateTimeOffset.UtcNow;
        }

        if (status == RegistrationStatus.Approved)
        {
            entity.ApprovedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class EfInvestorRegistrationStore : IInvestorRegistrationStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfInvestorRegistrationStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InvestorRegistration?> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.InvestorRegistrations
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.UserId == userId, cancellationToken);

        return entity?.ToModel();
    }

    public async Task<IReadOnlyCollection<InvestorRegistration>> ListAsync(RegistrationStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.InvestorRegistrations.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return items.Select(r => r.ToModel()).ToList();
    }

    public async Task<InvestorRegistration> SaveAsync(InvestorRegistration registration, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.InvestorRegistrations
            .SingleOrDefaultAsync(r => r.Id == registration.Id, cancellationToken);

        if (entity is null)
        {
            _dbContext.InvestorRegistrations.Add(registration.ToEntity());
        }
        else
        {
            entity.UserId = registration.UserId;
            entity.FullName = registration.FullName;
            entity.NationalId = registration.NationalId;
            entity.PhoneNumber = registration.PhoneNumber;
            entity.Status = registration.Status;
            entity.CreatedAt = registration.CreatedAt;
            entity.SubmittedAt = registration.SubmittedAt;
            entity.ApprovedAt = registration.ApprovedAt;
            entity.InterestAreas = registration.InterestAreas.ToList();
            entity.Notes = registration.Notes;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var updated = await _dbContext.InvestorRegistrations
            .AsNoTracking()
            .SingleAsync(r => r.Id == registration.Id, cancellationToken);

        return updated.ToModel();
    }

    public async Task UpdateStatusAsync(Guid id, RegistrationStatus status, string? notes, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.InvestorRegistrations
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (entity is null)
        {
            return;
        }

        entity.Status = status;
        entity.Notes = notes;

        if (status == RegistrationStatus.Submitted || status == RegistrationStatus.UnderReview)
        {
            entity.SubmittedAt ??= DateTimeOffset.UtcNow;
        }

        if (status == RegistrationStatus.Approved)
        {
            entity.ApprovedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class EfProjectWorkspaceStore : IProjectWorkspaceStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfProjectWorkspaceStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProjectWorkspace?> GetByParticipantAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ProjectWorkspaces
            .AsNoTracking()
            .Include(w => w.Tasks)
            .Include(w => w.StaffingRequests)
            .Include(w => w.Insights)
            .SingleOrDefaultAsync(w => w.ParticipantRegistrationId == participantId, cancellationToken);

        return entity?.ToModel();
    }

    public async Task<ProjectWorkspace> SaveAsync(ProjectWorkspace workspace, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.ProjectWorkspaces
            .Include(w => w.Tasks)
            .Include(w => w.StaffingRequests)
            .Include(w => w.Insights)
            .SingleOrDefaultAsync(w => w.Id == workspace.Id, cancellationToken);

        if (entity is null)
        {
            _dbContext.ProjectWorkspaces.Add(workspace.ToEntity());
        }
        else
        {
            entity.ParticipantRegistrationId = workspace.ParticipantRegistrationId;
            entity.ProjectName = workspace.ProjectName;
            entity.Vision = workspace.Vision;
            entity.BusinessModelSummary = workspace.BusinessModelSummary;

            entity.Tasks = workspace.Tasks.Select(task => new ProjectTaskEntity
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

            entity.StaffingRequests = workspace.StaffingRequests.Select(request => new StaffingRequestEntity
            {
                Id = request.Id,
                WorkspaceId = entity.Id,
                Skill = request.Skill,
                Description = request.Description,
                IsPaidOpportunity = request.IsPaidOpportunity,
                Status = request.Status,
                CreatedAt = request.CreatedAt
            }).ToList();

            entity.Insights = workspace.Insights.Select(insight => new AiInsightEntity
            {
                Id = insight.Id,
                WorkspaceId = entity.Id,
                Type = insight.Type,
                Summary = insight.Summary,
                ImprovementAreas = insight.ImprovementAreas,
                Confidence = insight.Confidence,
                CreatedAt = insight.CreatedAt
            }).ToList();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var updated = await _dbContext.ProjectWorkspaces
            .AsNoTracking()
            .Include(w => w.Tasks)
            .Include(w => w.StaffingRequests)
            .Include(w => w.Insights)
            .SingleAsync(w => w.Id == workspace.Id, cancellationToken);

        return updated.ToModel();
    }

    public async Task<ProjectTask> SaveTaskAsync(Guid workspaceId, ProjectTask task, CancellationToken cancellationToken = default)
    {
        var workspace = await _dbContext.ProjectWorkspaces
            .Include(w => w.Tasks)
            .SingleOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        if (workspace is null)
        {
            throw new InvalidOperationException("فضای پروژه یافت نشد.");
        }

        var existing = workspace.Tasks.SingleOrDefault(t => t.Id == task.Id);

        if (existing is null)
        {
            workspace.Tasks.Add(new ProjectTaskEntity
            {
                Id = task.Id,
                WorkspaceId = workspaceId,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                Assignee = task.Assignee,
                Status = task.Status,
                CreatedAt = task.CreatedAt,
                CompletedAt = task.CompletedAt
            });
        }
        else
        {
            existing.Title = task.Title;
            existing.Description = task.Description;
            existing.DueDate = task.DueDate;
            existing.Assignee = task.Assignee;
            existing.Status = task.Status;
            existing.CompletedAt = task.CompletedAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetByParticipantAsync(workspace.ParticipantRegistrationId, cancellationToken))!
            .Tasks.Single(t => t.Id == task.Id);
    }

    public async Task UpdateTaskStatusAsync(Guid workspaceId, Guid taskId, ProjectTaskStatus status, CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.ProjectTasks
            .SingleOrDefaultAsync(t => t.WorkspaceId == workspaceId && t.Id == taskId, cancellationToken);

        if (task is null)
        {
            return;
        }

        task.Status = status;

        if (status == ProjectTaskStatus.Completed)
        {
            task.CompletedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<StaffingRequest> AddStaffingRequestAsync(Guid workspaceId, StaffingRequest request, CancellationToken cancellationToken = default)
    {
        var workspace = await _dbContext.ProjectWorkspaces
            .Include(w => w.StaffingRequests)
            .SingleOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        if (workspace is null)
        {
            throw new InvalidOperationException("فضای پروژه یافت نشد.");
        }

        workspace.StaffingRequests.Add(new StaffingRequestEntity
        {
            Id = request.Id,
            WorkspaceId = workspaceId,
            Skill = request.Skill,
            Description = request.Description,
            IsPaidOpportunity = request.IsPaidOpportunity,
            Status = request.Status,
            CreatedAt = request.CreatedAt
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return request;
    }

    public async Task UpdateStaffingStatusAsync(Guid workspaceId, Guid requestId, StaffingRequestStatus status, CancellationToken cancellationToken = default)
    {
        var request = await _dbContext.StaffingRequests
            .SingleOrDefaultAsync(r => r.WorkspaceId == workspaceId && r.Id == requestId, cancellationToken);

        if (request is null)
        {
            return;
        }

        request.Status = status;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AiInsight> AddInsightAsync(Guid workspaceId, AiInsight insight, CancellationToken cancellationToken = default)
    {
        var workspace = await _dbContext.ProjectWorkspaces
            .Include(w => w.Insights)
            .SingleOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        if (workspace is null)
        {
            throw new InvalidOperationException("فضای پروژه یافت نشد.");
        }

        workspace.Insights.Add(new AiInsightEntity
        {
            Id = insight.Id,
            WorkspaceId = workspaceId,
            Type = insight.Type,
            Summary = insight.Summary,
            ImprovementAreas = insight.ImprovementAreas,
            Confidence = insight.Confidence,
            CreatedAt = insight.CreatedAt
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return insight;
    }
}

public class EfPostApprovalStore : IPostApprovalStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfPostApprovalStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaymentRecord> CreateOrUpdatePaymentAsync(PaymentRecord record, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.PaymentRecords
            .SingleOrDefaultAsync(p => p.RegistrationId == record.RegistrationId && p.RegistrationType == record.RegistrationType, cancellationToken);

        if (entity is null)
        {
            _dbContext.PaymentRecords.Add(record.ToEntity());
        }
        else
        {
            entity.Amount = record.Amount;
            entity.Status = record.Status;
            entity.GatewayUrl = record.GatewayUrl;
            entity.ReferenceCode = record.ReferenceCode;
            entity.PaidAt = record.PaidAt;
            entity.Notes = record.Notes;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var updated = await _dbContext.PaymentRecords
            .AsNoTracking()
            .SingleAsync(p => p.RegistrationId == record.RegistrationId && p.RegistrationType == record.RegistrationType, cancellationToken);

        return updated.ToModel();
    }

    public async Task<PaymentRecord?> GetPaymentAsync(Guid registrationId, string registrationType, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.PaymentRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.RegistrationId == registrationId && p.RegistrationType == registrationType, cancellationToken);

        return entity?.ToModel();
    }

    public async Task<NotificationLog> LogNotificationAsync(NotificationLog log, CancellationToken cancellationToken = default)
    {
        var entity = log.ToEntity();
        _dbContext.NotificationLogs.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    public async Task<IReadOnlyCollection<NotificationLog>> GetNotificationsAsync(Guid registrationId, string registrationType, CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.NotificationLogs
            .AsNoTracking()
            .Where(n => n.RegistrationId == registrationId && n.RegistrationType == registrationType)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync(cancellationToken);

        return items.Select(n => n.ToModel()).ToList();
    }
}
