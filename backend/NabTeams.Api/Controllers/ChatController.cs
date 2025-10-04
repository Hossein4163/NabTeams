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
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatRepository _chatRepository;
    private readonly IModerationService _moderationService;
    private readonly IModerationLogStore _moderationLogStore;
    private readonly IUserDisciplineStore _userDisciplineStore;
    private readonly IRateLimiter _rateLimiter;
    private readonly string _adminRole;

    public ChatController(
        IChatRepository chatRepository,
        IModerationService moderationService,
        IModerationLogStore moderationLogStore,
        IUserDisciplineStore userDisciplineStore,
        IRateLimiter rateLimiter,
        IOptions<AuthenticationSettings> authOptions)
    {
        _chatRepository = chatRepository;
        _moderationService = moderationService;
        _moderationLogStore = moderationLogStore;
        _userDisciplineStore = userDisciplineStore;
        _rateLimiter = rateLimiter;
        _adminRole = authOptions.Value.AdminRole;
    }

    [HttpGet("{role}/messages")]
    public async Task<ActionResult<MessagesResponse>> GetMessagesAsync(string role, CancellationToken cancellationToken)
    {
        if (!RoleChannelExtensions.TryParse(role, out var channel))
        {
            return BadRequest("نقش نامعتبر است.");
        }

        if (!HasChannelAccess(channel))
        {
            return Forbid();
        }

        var messages = await _chatRepository.GetMessagesAsync(channel, cancellationToken);
        return Ok(new MessagesResponse { Messages = messages });
    }

    [HttpPost("{role}/messages")]
    public async Task<ActionResult<SendMessageResponse>> SendMessageAsync(string role, [FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        if (!RoleChannelExtensions.TryParse(role, out var channel))
        {
            return BadRequest("نقش نامعتبر است.");
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("متن پیام الزامی است.");
        }

        if (!HasChannelAccess(channel))
        {
            return Forbid();
        }

        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var rateResult = _rateLimiter.CheckQuota(userId, channel);
        if (!rateResult.Allowed)
        {
            return StatusCode(429, new SendMessageResponse
            {
                MessageId = Guid.Empty,
                Status = MessageStatus.Blocked,
                ModerationRisk = 0,
                ModerationTags = Array.Empty<string>(),
                ModerationNotes = rateResult.Message,
                PenaltyPoints = 0,
                SoftWarn = false,
                RateLimitMessage = rateResult.Message
            });
        }

        var candidate = new MessageCandidate(userId, channel, request.Content);
        var moderation = await _moderationService.ModerateAsync(candidate, cancellationToken);

        var status = moderation.Decision switch
        {
            ModerationDecision.Publish or ModerationDecision.SoftWarn => MessageStatus.Published,
            ModerationDecision.Hold => MessageStatus.Held,
            _ => MessageStatus.Blocked
        };

        var message = new Message
        {
            Channel = channel,
            SenderUserId = userId,
            Content = request.Content,
            Status = status,
            ModerationRisk = moderation.RiskScore,
            ModerationTags = moderation.PolicyTags,
            ModerationNotes = moderation.Notes,
            PenaltyPoints = moderation.PenaltyPoints
        };

        await _chatRepository.AddMessageAsync(message, cancellationToken);

        var log = new ModerationLog
        {
            MessageId = message.Id,
            UserId = userId,
            Channel = channel,
            RiskScore = moderation.RiskScore,
            PolicyTags = moderation.PolicyTags,
            ActionTaken = moderation.Decision.ToString(),
            PenaltyPoints = moderation.PenaltyPoints
        };
        await _moderationLogStore.AddAsync(log, cancellationToken);

        if (moderation.PenaltyPoints != 0)
        {
            await _userDisciplineStore.UpdateScoreAsync(userId, channel, -moderation.PenaltyPoints, moderation.Notes, message.Id, cancellationToken);
        }

        var response = new SendMessageResponse
        {
            MessageId = message.Id,
            Status = status,
            ModerationRisk = moderation.RiskScore,
            ModerationTags = moderation.PolicyTags,
            ModerationNotes = moderation.Notes,
            PenaltyPoints = moderation.PenaltyPoints,
            SoftWarn = moderation.Decision == ModerationDecision.SoftWarn
        };

        return moderation.Decision switch
        {
            ModerationDecision.Publish or ModerationDecision.SoftWarn => Ok(response),
            ModerationDecision.Hold => StatusCode(StatusCodes.Status202Accepted, response),
            ModerationDecision.Block or ModerationDecision.BlockAndReport => StatusCode(StatusCodes.Status403Forbidden, response),
            _ => Ok(response)
        };
    }

    private string? GetUserId()
    {
        return User.FindFirstValue("sub")
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.Identity?.Name;
    }

    private bool HasChannelAccess(RoleChannel channel)
    {
        if (IsAdmin())
        {
            return true;
        }

        var normalized = channel.ToString().ToLowerInvariant();
        var roleClaims = User.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type.Equals("role", StringComparison.OrdinalIgnoreCase) || c.Type.Equals("roles", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value.ToLowerInvariant());

        return roleClaims.Contains(normalized);
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
