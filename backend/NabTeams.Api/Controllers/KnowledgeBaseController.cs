using Microsoft.AspNetCore.Mvc;
using NabTeams.Api.Models;
using NabTeams.Api.Services;

namespace NabTeams.Api.Controllers;

[ApiController]
[Route("api/knowledge-base")]
public class KnowledgeBaseController : ControllerBase
{
    private readonly ISupportKnowledgeBase _knowledgeBase;

    public KnowledgeBaseController(ISupportKnowledgeBase knowledgeBase)
    {
        _knowledgeBase = knowledgeBase;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<KnowledgeBaseItem>>> GetAsync(CancellationToken cancellationToken)
    {
        var items = await _knowledgeBase.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<KnowledgeBaseItem>> UpsertAsync([FromBody] KnowledgeBaseUpsertRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
        {
            return BadRequest("عنوان و متن منبع الزامی است.");
        }

        var item = new KnowledgeBaseItem
        {
            Id = string.IsNullOrWhiteSpace(request.Id) ? Guid.NewGuid().ToString() : request.Id,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            Audience = string.IsNullOrWhiteSpace(request.Audience) ? "all" : request.Audience.Trim().ToLowerInvariant(),
            Tags = request.Tags?.Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList() ?? new List<string>(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var stored = await _knowledgeBase.UpsertAsync(item, cancellationToken);
        return Ok(stored);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("شناسه نامعتبر است.");
        }

        await _knowledgeBase.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
