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
[Route("api/approvals")]
[Authorize]
public class ApprovalsController : ControllerBase
{
    private readonly IParticipantRegistrationStore _participantStore;
    private readonly IPostApprovalStore _postApprovalStore;

    public ApprovalsController(
        IParticipantRegistrationStore participantStore,
        IPostApprovalStore postApprovalStore)
    {
        _participantStore = participantStore;
        _postApprovalStore = postApprovalStore;
    }

    [HttpGet("participant/current/payment")]
    public async Task<ActionResult<PaymentRecord?>> GetCurrentParticipantPaymentAsync(CancellationToken cancellationToken)
    {
        var participant = await GetParticipantAsync(cancellationToken);
        if (participant is null)
        {
            return NotFound();
        }

        var payment = await _postApprovalStore.GetPaymentAsync(participant.Id, "participant", cancellationToken);
        return Ok(payment);
    }

    [HttpGet("participant/current/notifications")]
    public async Task<ActionResult<IReadOnlyCollection<NotificationLog>>> GetCurrentParticipantNotificationsAsync(CancellationToken cancellationToken)
    {
        var participant = await GetParticipantAsync(cancellationToken);
        if (participant is null)
        {
            return NotFound();
        }

        var notifications = await _postApprovalStore.GetNotificationsAsync(participant.Id, "participant", cancellationToken);
        return Ok(notifications);
    }

    [HttpPost("{type}/{id:guid}/payment")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<ActionResult<PaymentRecord>> CreateOrUpdatePaymentAsync(
        string type,
        Guid id,
        [FromBody] PaymentInstructionRequest request,
        CancellationToken cancellationToken)
    {
        var normalized = type.Trim().ToLowerInvariant();
        var record = new PaymentRecord
        {
            RegistrationId = id,
            RegistrationType = normalized,
            Amount = request.Amount,
            GatewayUrl = string.IsNullOrWhiteSpace(request.GatewayUrl)
                ? $"https://payments.example.com/{normalized}/{id}"
                : request.GatewayUrl,
            Status = PaymentStatus.AwaitingConfirmation,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var saved = await _postApprovalStore.CreateOrUpdatePaymentAsync(record, cancellationToken);
        return Ok(saved);
    }

    [HttpPut("{type}/{id:guid}/payment/status")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<IActionResult> UpdatePaymentStatusAsync(
        string type,
        Guid id,
        [FromBody] PaymentStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _postApprovalStore.GetPaymentAsync(id, type.Trim().ToLowerInvariant(), cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Status = request.Status;
        existing.ReferenceCode = request.ReferenceCode;
        if (request.Status == PaymentStatus.Paid)
        {
            existing.PaidAt = DateTimeOffset.UtcNow;
        }

        await _postApprovalStore.CreateOrUpdatePaymentAsync(existing, cancellationToken);
        return NoContent();
    }

    [HttpPost("{type}/{id:guid}/notifications")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<ActionResult<NotificationLog>> LogNotificationAsync(
        string type,
        Guid id,
        [FromBody] NotificationLogRequest request,
        CancellationToken cancellationToken)
    {
        var log = new NotificationLog
        {
            RegistrationId = id,
            RegistrationType = type.Trim().ToLowerInvariant(),
            Channel = request.Channel?.Trim() ?? "sms",
            Message = request.Message?.Trim() ?? string.Empty,
            SentAt = DateTimeOffset.UtcNow
        };

        var saved = await _postApprovalStore.LogNotificationAsync(log, cancellationToken);
        return Ok(saved);
    }

    [HttpGet("{type}/{id:guid}/notifications")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<ActionResult<IReadOnlyCollection<NotificationLog>>> GetNotificationsAsync(string type, Guid id, CancellationToken cancellationToken)
    {
        var items = await _postApprovalStore.GetNotificationsAsync(id, type.Trim().ToLowerInvariant(), cancellationToken);
        return Ok(items);
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
