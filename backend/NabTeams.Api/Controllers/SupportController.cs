using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NabTeams.Api.Models;
using NabTeams.Api.Services;

namespace NabTeams.Api.Controllers;

[ApiController]
[Route("api/support")]
[Authorize]
public class SupportController : ControllerBase
{
    private readonly ISupportResponder _responder;

    public SupportController(ISupportResponder responder)
    {
        _responder = responder;
    }

    [HttpPost("query")]
    public async Task<ActionResult<SupportAnswer>> QueryAsync([FromBody] SupportQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Question))
        {
            return BadRequest("سوال الزامی است.");
        }

        var role = string.IsNullOrWhiteSpace(query.Role) ? ResolveRoleFromClaims() : query.Role;
        var normalizedQuery = new SupportQuery
        {
            Question = query.Question,
            Role = string.IsNullOrWhiteSpace(role) ? "all" : role!
        };

        var answer = await _responder.GetAnswerAsync(normalizedQuery, cancellationToken);
        return Ok(answer);
    }

    private string? ResolveRoleFromClaims()
    {
        var roles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type.Equals("role", StringComparison.OrdinalIgnoreCase) || c.Type.Equals("roles", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value.ToLowerInvariant())
            .Where(value => !string.Equals(value, "admin", StringComparison.OrdinalIgnoreCase));

        return roles.FirstOrDefault();
    }
}
