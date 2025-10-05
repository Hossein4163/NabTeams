using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Common;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Web.Controllers;

[ApiController]
[Route("api/workspaces")]
[Authorize]
public class ProjectWorkspaceController : ControllerBase
{
    private readonly IParticipantRegistrationStore _participantStore;
    private readonly IProjectWorkspaceStore _workspaceStore;

    public ProjectWorkspaceController(
        IParticipantRegistrationStore participantStore,
        IProjectWorkspaceStore workspaceStore)
    {
        _participantStore = participantStore;
        _workspaceStore = workspaceStore;
    }

    [HttpGet("participant")]
    public async Task<ActionResult<ProjectWorkspace>> GetCurrentWorkspaceAsync(CancellationToken cancellationToken)
    {
        var participant = await GetParticipantAsync(cancellationToken);
        if (participant is null)
        {
            return NotFound("برای این کاربر ثبت‌نامی یافت نشد.");
        }

        var workspace = await _workspaceStore.GetByParticipantAsync(participant.Id, cancellationToken);

        if (workspace is null)
        {
            workspace = new ProjectWorkspace
            {
                Id = Guid.NewGuid(),
                ParticipantRegistrationId = participant.Id,
                ProjectName = participant.TeamName,
                Vision = participant.FinalSummary ?? string.Empty,
                BusinessModelSummary = participant.FinalSummary
            };

            workspace = await _workspaceStore.SaveAsync(workspace, cancellationToken);
        }

        return Ok(workspace);
    }

    [HttpPost("participant")]
    public async Task<ActionResult<ProjectWorkspace>> UpsertWorkspaceAsync(
        [FromBody] ProjectWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        var participant = await GetParticipantAsync(cancellationToken);
        if (participant is null)
        {
            return NotFound("برای این کاربر ثبت‌نامی یافت نشد.");
        }

        var workspace = await _workspaceStore.GetByParticipantAsync(participant.Id, cancellationToken)
            ?? new ProjectWorkspace
            {
                Id = Guid.NewGuid(),
                ParticipantRegistrationId = participant.Id
            };

        workspace.ProjectName = request.ProjectName?.Trim() ?? workspace.ProjectName;
        workspace.Vision = request.Vision?.Trim() ?? workspace.Vision;
        workspace.BusinessModelSummary = request.BusinessModelSummary?.Trim() ?? workspace.BusinessModelSummary;

        var saved = await _workspaceStore.SaveAsync(workspace, cancellationToken);
        return Ok(saved);
    }

    [HttpPost("participant/tasks")]
    public async Task<ActionResult<ProjectTask>> AddTaskAsync(
        [FromBody] ProjectTaskRequest request,
        CancellationToken cancellationToken)
    {
        var participant = await GetParticipantAsync(cancellationToken);
        if (participant is null)
        {
            return NotFound();
        }

        var workspace = await _workspaceStore.GetByParticipantAsync(participant.Id, cancellationToken);
        if (workspace is null)
        {
            return BadRequest("ابتدا فضای پروژه را ایجاد کنید.");
        }

        var task = new ProjectTask
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            Title = request.Title?.Trim() ?? string.Empty,
            Description = request.Description?.Trim() ?? string.Empty,
            DueDate = request.DueDate,
            Assignee = request.Assignee?.Trim() ?? string.Empty,
            Status = ProjectTaskStatus.Planned,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var saved = await _workspaceStore.SaveTaskAsync(workspace.Id, task, cancellationToken);
        return Ok(saved);
    }

    [HttpPut("participant/tasks/{taskId:guid}/status")]
    public async Task<IActionResult> UpdateTaskStatusAsync(Guid taskId, [FromBody] ProjectTaskStatus status, CancellationToken cancellationToken)
    {
        var participant = await GetParticipantAsync(cancellationToken);
        if (participant is null)
        {
            return NotFound();
        }

        var workspace = await _workspaceStore.GetByParticipantAsync(participant.Id, cancellationToken);
        if (workspace is null)
        {
            return NotFound();
        }

        await _workspaceStore.UpdateTaskStatusAsync(workspace.Id, taskId, status, cancellationToken);
        return NoContent();
    }

    [HttpPost("participant/staffing")]
    public async Task<ActionResult<StaffingRequest>> AddStaffingRequestAsync(
        [FromBody] StaffingRequestCreate request,
        CancellationToken cancellationToken)
    {
        var participant = await GetParticipantAsync(cancellationToken);
        if (participant is null)
        {
            return NotFound();
        }

        var workspace = await _workspaceStore.GetByParticipantAsync(participant.Id, cancellationToken);
        if (workspace is null)
        {
            return BadRequest("ابتدا فضای پروژه را ایجاد کنید.");
        }

        var staffing = new StaffingRequest
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            Skill = request.Skill?.Trim() ?? string.Empty,
            Description = request.Description?.Trim() ?? string.Empty,
            IsPaidOpportunity = request.IsPaidOpportunity,
            Status = StaffingRequestStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var saved = await _workspaceStore.AddStaffingRequestAsync(workspace.Id, staffing, cancellationToken);
        return Ok(saved);
    }

    [HttpPut("participant/staffing/{id:guid}/status")]
    public async Task<IActionResult> UpdateStaffingStatusAsync(Guid id, [FromBody] StaffingRequestStatus status, CancellationToken cancellationToken)
    {
        var participant = await GetParticipantAsync(cancellationToken);
        if (participant is null)
        {
            return NotFound();
        }

        var workspace = await _workspaceStore.GetByParticipantAsync(participant.Id, cancellationToken);
        if (workspace is null)
        {
            return NotFound();
        }

        await _workspaceStore.UpdateStaffingStatusAsync(workspace.Id, id, status, cancellationToken);
        return NoContent();
    }

    [HttpPost("participant/insights")]
    public async Task<ActionResult<AiInsight>> AddInsightAsync(
        [FromBody] AiInsightRequest request,
        CancellationToken cancellationToken)
    {
        var participant = await GetParticipantAsync(cancellationToken);
        if (participant is null)
        {
            return NotFound();
        }

        var workspace = await _workspaceStore.GetByParticipantAsync(participant.Id, cancellationToken);
        if (workspace is null)
        {
            return BadRequest("ابتدا فضای پروژه را ایجاد کنید.");
        }

        var insight = new AiInsight
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            Type = request.Type,
            Summary = request.Summary?.Trim() ?? string.Empty,
            ImprovementAreas = request.ImprovementAreas?.Trim() ?? string.Empty,
            Confidence = request.Confidence,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var saved = await _workspaceStore.AddInsightAsync(workspace.Id, insight, cancellationToken);
        return Ok(saved);
    }

    private async Task<ParticipantRegistration?> GetParticipantAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return await _participantStore.GetByUserAsync(userId, cancellationToken);
    }

    private string? GetUserId()
    {
        return User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.Identity?.Name;
    }
}
