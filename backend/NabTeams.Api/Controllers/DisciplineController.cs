using Microsoft.AspNetCore.Mvc;
using NabTeams.Api.Models;
using NabTeams.Api.Stores;

namespace NabTeams.Api.Controllers;

[ApiController]
[Route("api/discipline")]
public class DisciplineController : ControllerBase
{
    private readonly IUserDisciplineStore _disciplineStore;

    public DisciplineController(IUserDisciplineStore disciplineStore)
    {
        _disciplineStore = disciplineStore;
    }

    [HttpGet("{role}/{userId}")]
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
}
