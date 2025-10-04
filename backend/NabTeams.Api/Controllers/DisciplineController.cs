using Microsoft.AspNetCore.Mvc;
using NabTeams.Api.Models;
using NabTeams.Api.Stores;

namespace NabTeams.Api.Controllers;

[ApiController]
[Route("api/discipline")]
[Authorize]
public class DisciplineController : ControllerBase
{
    private readonly IUserDisciplineStore _disciplineStore;

    public DisciplineController(IUserDisciplineStore disciplineStore)
    {
        _disciplineStore = disciplineStore;
    }

    [HttpGet("{role}/me")]
    public async Task<ActionResult<UserDiscipline>> GetForCurrentAsync(string role, CancellationToken cancellationToken)
    {
        if (!RoleChannelExtensions.TryParse(role, out var channel))
        {
            return BadRequest("نقش نامعتبر است.");
        }

        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var record = await _disciplineStore.GetAsync(userId, channel, cancellationToken);
        if (record is null)
        {
            return NotFound();
        }

        return Ok(record);
    }

    [HttpGet("{role}/{userId}")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public async Task<ActionResult<UserDiscipline>> GetAsync(string role, string userId, CancellationToken cancellationToken)
    {
        if (!RoleChannelExtensions.TryParse(role, out var channel))
        {
            return BadRequest("نقش نامعتبر است.");
        }

        var record = await _disciplineStore.GetAsync(userId, channel, cancellationToken);
        if (record is null)
        {
            return NotFound();
        }

        return Ok(record);
    }

    private string? GetUserId()
    {
        return User.FindFirstValue("sub")
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.Identity?.Name;
    }
}
