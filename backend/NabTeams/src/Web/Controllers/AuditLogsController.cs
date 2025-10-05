using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
using NabTeams.Web.Configuration;
using Swashbuckle.AspNetCore.Annotations;

namespace NabTeams.Web.Controllers;

[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Policy = AuthorizationPolicies.Admin)]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [SwaggerOperation(Summary = "فهرست رویدادهای ثبت شده", Description = "لیست اقدامات انجام‌شده توسط ادمین‌ها و کاربران را برای پایش و ممیزی باز می‌گرداند.")]
    [ProducesResponseType(typeof(IEnumerable<AuditLogResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AuditLogResponse>>> ListAsync(
        [FromQuery] string? entityType,
        [FromQuery] string? entityId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        var logs = await _auditLogService.ListAsync(entityType, entityId, skip, take, cancellationToken);
        var response = logs.Select(AuditLogResponse.FromEntity).ToList();
        return Ok(response);
    }

    public sealed record AuditLogResponse(
        Guid Id,
        string ActorId,
        string ActorName,
        string Action,
        string EntityType,
        string EntityId,
        DateTimeOffset CreatedAt,
        object? Metadata)
    {
        public static AuditLogResponse FromEntity(AuditLogEntry entry)
        {
            object? metadata = null;
            if (!string.IsNullOrWhiteSpace(entry.Metadata))
            {
                try
                {
                    metadata = JsonSerializer.Deserialize<JsonElement>(entry.Metadata);
                }
                catch (JsonException)
                {
                    metadata = entry.Metadata;
                }
            }

            return new AuditLogResponse(
                entry.Id,
                entry.ActorId,
                entry.ActorName,
                entry.Action,
                entry.EntityType,
                entry.EntityId,
                entry.CreatedAt,
                metadata);
        }
    }
}
