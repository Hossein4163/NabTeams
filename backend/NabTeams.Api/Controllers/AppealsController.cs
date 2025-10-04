using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NabTeams.Api.Configuration;
using NabTeams.Api.Models;
using NabTeams.Api.Services;
using NabTeams.Api.Stores;

namespace NabTeams.Api.Controllers;

[ApiController]
[Route("api/appeals")]
[Authorize]
public class AppealsController : ControllerBase
{
    private readonly IAppealStore _appealStore;
    private readonly IChatRepository _chatRepository;
    private readonly IModerationLogStore _moderationLogStore;
    private readonly string _adminRole;

    public AppealsController(
        IAppealStore appealStore,
        IChatRepository chatRepository,
        IModerationLogStore moderationLogStore,
        IOptions<AuthenticationSettings> authOptions)
    {
        _appealStore = appealStore;
        _chatRepository = chatRepository;
        _moderationLogStore = moderationLogStore;
        _adminRole = authOptions.Value.AdminRole;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<Appeal>>> GetMineAsync(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var appeals = await _appealStore.GetForUserAsync(userId, cancellationToken);
        return Ok(appeals);
    }

    [HttpPost]
    public async Task<ActionResult<Appeal>> CreateAsync([FromBody] CreateAppealRequest request, CancellationToken cancellationToken)
    {
        if (request.MessageId == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest("شناسه پیام و دلیل اعتراض الزامی است.");
        }

        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var message = await _chatRepository.GetMessageAsync(request.MessageId, cancellationToken);
        var log = message is null
            ? await _moderationLogStore.GetAsync(request.MessageId, cancellationToken)
            : null;

        if (message is null && log is null)
        {
            return NotFound("پیام موردنظر یافت نشد.");
        }

        var channel = message?.Channel ?? log!.Channel;
        var ownerId = message?.SenderUserId ?? log!.UserId;
        if (!string.Equals(ownerId, userId, StringComparison.Ordinal) && !IsAdmin())
        {
            return Forbid();
        }

        var appeal = new Appeal
        {
            MessageId = request.MessageId,
            Channel = channel,
            UserId = ownerId,
            Reason = request.Reason.Trim(),
            SubmittedAt = DateTimeOffset.UtcNow,
            Status = AppealStatus.Pending
        };

        try
        {
            var created = await _appealStore.CreateAsync(appeal, cancellationToken);
            return CreatedAtAction(nameof(GetMineAsync), new { id = created.Id }, created);
        }
        catch (Exception ex) when (ex is Microsoft.EntityFrameworkCore.DbUpdateException or InvalidOperationException)
        {
            return Conflict("برای این پیام قبلاً اعتراض ثبت شده است.");
        }
    }

    [HttpGet("admin")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<ActionResult<IReadOnlyCollection<Appeal>>> QueryAsync([FromQuery] string? role, [FromQuery] AppealStatus? status, CancellationToken cancellationToken)
    {
        RoleChannel? channel = null;
        if (!string.IsNullOrWhiteSpace(role))
        {
            if (!RoleChannelExtensions.TryParse(role, out var parsed))
            {
                return BadRequest("نقش نامعتبر است.");
            }

            channel = parsed;
        }

        var appeals = await _appealStore.QueryAsync(channel, status, cancellationToken);
        return Ok(appeals);
    }

    [HttpPost("{id:guid}/decision")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<ActionResult<Appeal>> ResolveAsync(Guid id, [FromBody] AppealDecisionRequest request, CancellationToken cancellationToken)
    {
        if (request.Status == AppealStatus.Pending)
        {
            return BadRequest("وضعیت باید پذیرفته شده یا رد شده باشد.");
        }

        var reviewerId = GetUserId() ?? "system";
        var resolved = await _appealStore.ResolveAsync(id, request.Status, reviewerId, request.Notes, cancellationToken);
        if (resolved is null)
        {
            return NotFound();
        }

        return Ok(resolved);
    }

    private string? GetUserId()
    {
        return User.FindFirstValue("sub")
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.Identity?.Name;
    }

    private bool IsAdmin()
    {
        if (User.IsInRole(_adminRole))
        {
            return true;
        }

        var normalizedAdmin = _adminRole.ToLowerInvariant();
        return User.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type.Equals("role", StringComparison.OrdinalIgnoreCase) || c.Type.Equals("roles", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value.ToLowerInvariant())
            .Contains(normalizedAdmin);
    }
}
