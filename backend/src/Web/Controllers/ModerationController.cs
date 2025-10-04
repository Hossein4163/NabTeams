using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using NabTeams.Web.Configuration;

namespace NabTeams.Web.Controllers;

[ApiController]
[Route("api/moderation")]
[Authorize(Policy = AuthorizationPolicies.Admin)]
public class ModerationController : ControllerBase
{
    private readonly IModerationLogStore _logStore;

    public ModerationController(IModerationLogStore logStore)
    {
        _logStore = logStore;
    }

    [HttpGet("{role}/logs")]
    public async Task<ActionResult<IReadOnlyCollection<ModerationLog>>> GetLogsAsync(string role, CancellationToken cancellationToken)
    {
        if (!RoleChannelExtensions.TryParse(role, out var channel))
        {
            return BadRequest("نقش نامعتبر است.");
        }

        var logs = await _logStore.QueryAsync(channel, cancellationToken);
        return Ok(logs);
    }
}
