using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using NabTeams.Web.Configuration;
using Swashbuckle.AspNetCore.Annotations;

namespace NabTeams.Web.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "فهرست رویدادها", Description = "لیست رویدادهای فعال و وضعیت فعال‌سازی تسک‌منیجر را بازمی‌گرداند.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<EventResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        var events = await _eventService.ListAsync(cancellationToken);
        return Ok(events.Select(EventResponse.FromDomain).ToList());
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "جزئیات یک رویداد", Description = "رویداد انتخاب‌شده را به‌همراه وضعیت فعال بودن تسک‌منیجر بازمی‌گرداند.")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var eventDetail = await _eventService.GetAsync(id, cancellationToken);
        if (eventDetail is null)
        {
            return NotFound();
        }

        return Ok(EventResponse.FromDomain(eventDetail));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [SwaggerOperation(Summary = "ایجاد رویداد", Description = "رویداد جدید را با گزینهٔ فعال‌سازی تسک‌منیجر ایجاد می‌کند.")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<EventResponse>> CreateAsync([FromBody] EventRequest request, CancellationToken cancellationToken)
    {
        var model = request.ToDomain();
        var created = await _eventService.CreateAsync(model, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, EventResponse.FromDomain(created));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [SwaggerOperation(Summary = "به‌روزرسانی رویداد", Description = "نام، توضیحات، زمان‌بندی و وضعیت فعال بودن تسک‌منیجر را ویرایش می‌کند.")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> UpdateAsync(Guid id, [FromBody] EventRequest request, CancellationToken cancellationToken)
    {
        var model = request.ToDomain(id);
        var updated = await _eventService.UpdateAsync(id, model, cancellationToken);
        if (updated is null)
        {
            return NotFound();
        }

        return Ok(EventResponse.FromDomain(updated));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [SwaggerOperation(Summary = "حذف رویداد", Description = "رویداد را حذف می‌کند. اگر تیم‌هایی به رویداد متصل باشند عملیات با خطای دیتابیس متوقف می‌شود.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _eventService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    public record EventRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; init; } = string.Empty;

        [MaxLength(1024)]
        public string? Description { get; init; }
            = null;

        public DateTimeOffset? StartsAt { get; init; }
            = null;

        public DateTimeOffset? EndsAt { get; init; }
            = null;

        public bool AiTaskManagerEnabled { get; init; }
            = false;

        public EventDetail ToDomain(Guid? id = null)
            => new()
            {
                Id = id ?? Guid.Empty,
                Name = Name.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                StartsAt = StartsAt,
                EndsAt = EndsAt,
                AiTaskManagerEnabled = AiTaskManagerEnabled
            };
    }

    public record EventResponse
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
            = null;
        public DateTimeOffset? StartsAt { get; init; }
            = null;
        public DateTimeOffset? EndsAt { get; init; }
            = null;
        public bool AiTaskManagerEnabled { get; init; }
            = false;
        public IReadOnlyCollection<ParticipantTaskPreview> SampleTasks { get; init; }
            = Array.Empty<ParticipantTaskPreview>();

        public static EventResponse FromDomain(EventDetail detail)
            => new()
            {
                Id = detail.Id,
                Name = detail.Name,
                Description = detail.Description,
                StartsAt = detail.StartsAt,
                EndsAt = detail.EndsAt,
                AiTaskManagerEnabled = detail.AiTaskManagerEnabled,
                SampleTasks = detail.SampleTasks
                    .Select(task => new ParticipantTaskPreview
                    {
                        Title = task.Title,
                        Status = task.Status,
                        DueAt = task.DueAt,
                        AssignedTo = task.AssignedTo
                    })
                    .ToList()
            };
    }

    public record ParticipantTaskPreview
    {
        public string Title { get; init; } = string.Empty;
        public ParticipantTaskStatus Status { get; init; }
            = ParticipantTaskStatus.Todo;
        public DateTimeOffset? DueAt { get; init; }
            = null;
        public string? AssignedTo { get; init; }
            = null;
    }
}
