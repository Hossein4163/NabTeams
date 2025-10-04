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
    private readonly IRateLimiter _rateLimiter;
    private readonly IChatModerationQueue _moderationQueue;
    private readonly string _adminRole;

    public ChatController(
        IChatRepository chatRepository,
        IRateLimiter rateLimiter,
        IChatModerationQueue moderationQueue,
        IOptions<AuthenticationSettings> authOptions)
    {
        _chatRepository = chatRepository;
        _rateLimiter = rateLimiter;
        _moderationQueue = moderationQueue;
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

        var content = request.Content?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(content))
        {
            return BadRequest("متن پیام الزامی است.");
        }

        if (content.Length > SendMessageRequest.MaxContentLength)
        {
            return BadRequest($"حداکثر طول پیام {SendMessageRequest.MaxContentLength} نویسه است.");
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

        var message = new Message
        {
            Channel = channel,
            SenderUserId = userId,
            Content = content,
            Status = MessageStatus.Held,
            ModerationRisk = 0,
            ModerationTags = Array.Empty<string>(),
            ModerationNotes = "در انتظار بررسی خودکار توسط Gemini.",
            PenaltyPoints = 0
        };

        await _chatRepository.AddMessageAsync(message, cancellationToken);
        await _moderationQueue.EnqueueAsync(new ChatModerationWorkItem(message.Id, userId, channel, content), cancellationToken);

        var response = new SendMessageResponse
        {
            MessageId = message.Id,
            Status = MessageStatus.Held,
            ModerationRisk = 0,
            ModerationTags = Array.Empty<string>(),
            ModerationNotes = message.ModerationNotes,
            PenaltyPoints = 0,
            SoftWarn = false
        };

        return StatusCode(StatusCodes.Status202Accepted, response);
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
