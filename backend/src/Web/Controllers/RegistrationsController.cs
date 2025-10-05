using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Common;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using NabTeams.Web.Configuration;

namespace NabTeams.Web.Controllers;

[ApiController]
[Route("api/registrations")]
[Authorize]
public class RegistrationsController : ControllerBase
{
    private readonly IParticipantRegistrationStore _participantStore;
    private readonly IJudgeRegistrationStore _judgeStore;
    private readonly IInvestorRegistrationStore _investorStore;
    private readonly IProjectWorkspaceStore _workspaceStore;
    private readonly IPostApprovalStore _postApprovalStore;

    public RegistrationsController(
        IParticipantRegistrationStore participantStore,
        IJudgeRegistrationStore judgeStore,
        IInvestorRegistrationStore investorStore,
        IProjectWorkspaceStore workspaceStore,
        IPostApprovalStore postApprovalStore)
    {
        _participantStore = participantStore;
        _judgeStore = judgeStore;
        _investorStore = investorStore;
        _workspaceStore = workspaceStore;
        _postApprovalStore = postApprovalStore;
    }

    [HttpGet("participant")]
    public async Task<ActionResult<ParticipantRegistration>> GetParticipantAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var registration = await _participantStore.GetByUserAsync(userId, cancellationToken);

        if (registration is null)
        {
            registration = new ParticipantRegistration
            {
                UserId = userId
            };
            registration = await _participantStore.SaveAsync(registration, cancellationToken);
        }

        return Ok(registration);
    }

    [HttpPut("participant/head")]
    public async Task<ActionResult<ParticipantRegistration>> UpsertParticipantHeadAsync(
        [FromBody] ParticipantHeadStepRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var registration = await _participantStore.GetByUserAsync(userId, cancellationToken)
            ?? new ParticipantRegistration { UserId = userId };

        registration.HeadFullName = request.FullName?.Trim() ?? string.Empty;
        registration.HeadNationalId = request.NationalId?.Trim() ?? string.Empty;
        registration.HeadPhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
        registration.HeadBirthDate = request.BirthDate;
        registration.HeadDegree = request.Degree?.Trim() ?? string.Empty;
        registration.HeadMajor = request.Major?.Trim() ?? string.Empty;
        registration.TeamName = request.TeamName?.Trim() ?? string.Empty;
        registration.HasTeam = request.HasTeam;
        registration.Status = RegistrationStatus.Draft;
        registration.Stage = AdvanceStage(registration.Stage, ParticipantRegistrationStage.TeamDetails);

        var updated = await _participantStore.SaveAsync(registration, cancellationToken);
        return Ok(updated);
    }

    [HttpPut("participant/team")]
    public async Task<ActionResult<ParticipantRegistration>> UpsertParticipantTeamAsync(
        [FromBody] ParticipantTeamStepRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var registration = await _participantStore.GetByUserAsync(userId, cancellationToken);
        if (registration is null)
        {
            return BadRequest("ابتدا اطلاعات سرپرست تیم را ثبت کنید.");
        }

        registration.TeamCompleted = request.TeamCompleted;
        registration.TeamMembers.Clear();

        foreach (var member in request.Members ?? Array.Empty<TeamMemberRequest>())
        {
            registration.TeamMembers.Add(new TeamMember
            {
                Id = Guid.NewGuid(),
                FullName = member.FullName?.Trim() ?? string.Empty,
                Role = member.Role?.Trim() ?? string.Empty,
                FocusArea = member.FocusArea?.Trim() ?? string.Empty
            });
        }

        registration.Stage = AdvanceStage(registration.Stage, ParticipantRegistrationStage.Documents);

        var updated = await _participantStore.SaveAsync(registration, cancellationToken);
        return Ok(updated);
    }

    [HttpPut("participant/documents")]
    public async Task<ActionResult<ParticipantRegistration>> UpsertParticipantDocumentsAsync(
        [FromBody] ParticipantDocumentsStepRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var registration = await _participantStore.GetByUserAsync(userId, cancellationToken);
        if (registration is null)
        {
            return BadRequest("ابتدا مراحل قبلی را تکمیل کنید.");
        }

        registration.ProjectFileUrl = request.ProjectFileUrl?.Trim();
        registration.ResumeFileUrl = request.ResumeFileUrl?.Trim();
        registration.SocialLinks = request.SocialLinks?
            .Where(link => !string.IsNullOrWhiteSpace(link))
            .Select(link => link.Trim())
            .Distinct()
            .ToList() ?? new List<string>();

        registration.Stage = AdvanceStage(registration.Stage, ParticipantRegistrationStage.ReadyForReview);

        var updated = await _participantStore.SaveAsync(registration, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("participant/submit")]
    public async Task<ActionResult<ParticipantRegistration>> SubmitParticipantAsync(
        [FromBody] ParticipantSubmitRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var registration = await _participantStore.GetByUserAsync(userId, cancellationToken);
        if (registration is null)
        {
            return BadRequest("هیچ پرونده‌ای برای ارسال وجود ندارد.");
        }

        registration.FinalSummary = request.FinalSummary?.Trim() ?? string.Empty;
        registration.Stage = ParticipantRegistrationStage.Completed;
        registration.Status = RegistrationStatus.Submitted;
        registration.SubmittedAt ??= DateTimeOffset.UtcNow;

        var updated = await _participantStore.SaveAsync(registration, cancellationToken);
        return Ok(updated);
    }

    [HttpGet("participant/admin")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<ActionResult<IReadOnlyCollection<ParticipantRegistration>>> QueryParticipantsAsync(
        [FromQuery] RegistrationStatus? status,
        CancellationToken cancellationToken)
    {
        var items = await _participantStore.ListAsync(status, cancellationToken);
        return Ok(items);
    }

    [HttpPost("participant/{id:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<IActionResult> UpdateParticipantStatusAsync(Guid id, [FromBody] RegistrationStatusRequest request, CancellationToken cancellationToken)
    {
        await _participantStore.UpdateStatusAsync(id, request.Status, request.Notes, cancellationToken);

        if (request.Status == RegistrationStatus.Approved)
        {
            var registration = await _participantStore.GetByIdAsync(id, cancellationToken);
            if (registration is not null)
            {
                var payment = await _postApprovalStore.GetPaymentAsync(id, "participant", cancellationToken);
                if (payment is null)
                {
                    await _postApprovalStore.CreateOrUpdatePaymentAsync(new PaymentRecord
                    {
                        RegistrationId = id,
                        RegistrationType = "participant",
                        Amount = 0m,
                        Status = PaymentStatus.AwaitingConfirmation,
                        GatewayUrl = $"https://payments.example.com/invoice/{id}",
                        Notes = request.Notes
                    }, cancellationToken);
                }

                await _postApprovalStore.LogNotificationAsync(new NotificationLog
                {
                    RegistrationId = id,
                    RegistrationType = "participant",
                    Channel = "sms",
                    Message = "تیم شما تأیید شد. برای ادامه مراحل، پرداخت ورود به مرحله دوم را تکمیل کنید.",
                    SentAt = DateTimeOffset.UtcNow
                }, cancellationToken);

                if (registration.WorkspaceId is null)
                {
                    var workspace = new ProjectWorkspace
                    {
                        Id = Guid.NewGuid(),
                        ParticipantRegistrationId = registration.Id,
                        ProjectName = registration.TeamName,
                        Vision = registration.FinalSummary ?? string.Empty,
                        BusinessModelSummary = registration.FinalSummary
                    };

                    registration.WorkspaceId = workspace.Id;
                    await _workspaceStore.SaveAsync(workspace, cancellationToken);
                    await _participantStore.SaveAsync(registration, cancellationToken);
                }
            }
        }

        return NoContent();
    }

    [HttpGet("judge")]
    public async Task<ActionResult<JudgeRegistration>> GetJudgeAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var registration = await _judgeStore.GetByUserAsync(userId, cancellationToken)
            ?? new JudgeRegistration { UserId = userId };

        if (registration.Id == Guid.Empty)
        {
            registration.Id = Guid.NewGuid();
        }

        return Ok(registration);
    }

    [HttpPut("judge")]
    public async Task<ActionResult<JudgeRegistration>> UpsertJudgeAsync(
        [FromBody] JudgeRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var registration = await _judgeStore.GetByUserAsync(userId, cancellationToken)
            ?? new JudgeRegistration { UserId = userId };

        registration.FullName = request.FullName?.Trim() ?? string.Empty;
        registration.NationalId = request.NationalId?.Trim() ?? string.Empty;
        registration.PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
        registration.BirthDate = request.BirthDate;
        registration.ExpertiseArea = request.ExpertiseArea?.Trim() ?? string.Empty;
        registration.HighestDegree = request.HighestDegree?.Trim() ?? string.Empty;
        registration.Status = RegistrationStatus.Submitted;
        registration.SubmittedAt ??= DateTimeOffset.UtcNow;

        var saved = await _judgeStore.SaveAsync(registration, cancellationToken);
        return Ok(saved);
    }

    [HttpGet("judge/admin")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<ActionResult<IReadOnlyCollection<JudgeRegistration>>> QueryJudgesAsync(
        [FromQuery] RegistrationStatus? status,
        CancellationToken cancellationToken)
    {
        var items = await _judgeStore.ListAsync(status, cancellationToken);
        return Ok(items);
    }

    [HttpPost("judge/{id:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<IActionResult> UpdateJudgeStatusAsync(Guid id, [FromBody] RegistrationStatusRequest request, CancellationToken cancellationToken)
    {
        await _judgeStore.UpdateStatusAsync(id, request.Status, request.Notes, cancellationToken);
        return NoContent();
    }

    [HttpGet("investor")]
    public async Task<ActionResult<InvestorRegistration>> GetInvestorAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var registration = await _investorStore.GetByUserAsync(userId, cancellationToken)
            ?? new InvestorRegistration { UserId = userId };

        return Ok(registration);
    }

    [HttpPut("investor")]
    public async Task<ActionResult<InvestorRegistration>> UpsertInvestorAsync(
        [FromBody] InvestorRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var registration = await _investorStore.GetByUserAsync(userId, cancellationToken)
            ?? new InvestorRegistration { UserId = userId };

        registration.FullName = request.FullName?.Trim() ?? string.Empty;
        registration.NationalId = request.NationalId?.Trim() ?? string.Empty;
        registration.PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty;
        registration.InterestAreas = request.InterestAreas?
            .Where(area => !string.IsNullOrWhiteSpace(area))
            .Select(area => area.Trim())
            .Distinct()
            .ToList() ?? new List<string>();
        registration.Status = RegistrationStatus.Submitted;
        registration.SubmittedAt ??= DateTimeOffset.UtcNow;

        var saved = await _investorStore.SaveAsync(registration, cancellationToken);
        return Ok(saved);
    }

    [HttpGet("investor/admin")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<ActionResult<IReadOnlyCollection<InvestorRegistration>>> QueryInvestorsAsync(
        [FromQuery] RegistrationStatus? status,
        CancellationToken cancellationToken)
    {
        var items = await _investorStore.ListAsync(status, cancellationToken);
        return Ok(items);
    }

    [HttpPost("investor/{id:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<IActionResult> UpdateInvestorStatusAsync(Guid id, [FromBody] RegistrationStatusRequest request, CancellationToken cancellationToken)
    {
        await _investorStore.UpdateStatusAsync(id, request.Status, request.Notes, cancellationToken);
        return NoContent();
    }

    private string? GetUserId()
    {
        return User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.Identity?.Name;
    }

    private static ParticipantRegistrationStage AdvanceStage(ParticipantRegistrationStage current, ParticipantRegistrationStage target)
    {
        return (ParticipantRegistrationStage)Math.Max((int)current, (int)target);
    }
}
