using Microsoft.AspNetCore.Mvc;
using NabTeams.Api.Models;
using NabTeams.Api.Services;

namespace NabTeams.Api.Controllers;

[ApiController]
[Route("api/support")]
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
        if (string.IsNullOrWhiteSpace(query.UserId) || string.IsNullOrWhiteSpace(query.Question))
        {
            return BadRequest("شناسه کاربر و سوال الزامی است.");
        }

        var answer = await _responder.GetAnswerAsync(query, cancellationToken);
        return Ok(answer);
    }
}
