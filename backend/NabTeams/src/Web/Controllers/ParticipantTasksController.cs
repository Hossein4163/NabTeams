using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Tasks.Models;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace NabTeams.Web.Controllers;

[ApiController]
[Route("api/registrations/participants/{participantId:guid}/tasks")]
public class ParticipantTasksController : ControllerBase
{
    private readonly IParticipantTaskService _taskService;

    public ParticipantTasksController(IParticipantTaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    [SwaggerOperation(Summary = "فهرست تسک‌های تیم", Description = "تمامی تسک‌های ثبت‌شده برای تیم شرکت‌کننده را بازمی‌گرداند.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<RegistrationsController.ParticipantTaskResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<RegistrationsController.ParticipantTaskResponse>>> ListAsync(Guid participantId, CancellationToken cancellationToken)
    {
        try
        {
            var tasks = await _taskService.ListAsync(participantId, cancellationToken);
            return Ok(tasks.Select(RegistrationsController.ParticipantTaskResponse.FromDomain).ToList());
        }
        catch (InvalidOperationException ex)
        {
            return BuildBadRequest(ex);
        }
    }

    [HttpPost]
    [SwaggerOperation(Summary = "ایجاد تسک", Description = "تسک جدیدی را برای تیم ثبت می‌کند.")]
    [ProducesResponseType(typeof(RegistrationsController.ParticipantTaskResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<RegistrationsController.ParticipantTaskResponse>> CreateAsync(Guid participantId, [FromBody] ParticipantTaskRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _taskService.CreateAsync(participantId, request.ToInput(), cancellationToken);
            return CreatedAtAction(nameof(ListAsync), new { participantId }, RegistrationsController.ParticipantTaskResponse.FromDomain(created));
        }
        catch (InvalidOperationException ex)
        {
            return BuildBadRequest(ex);
        }
    }

    [HttpPut("{taskId:guid}")]
    [SwaggerOperation(Summary = "ویرایش تسک", Description = "عنوان، توضیح، تاریخ سررسید یا مسئول تسک را ویرایش می‌کند.")]
    [ProducesResponseType(typeof(RegistrationsController.ParticipantTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegistrationsController.ParticipantTaskResponse>> UpdateAsync(Guid participantId, Guid taskId, [FromBody] ParticipantTaskRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _taskService.UpdateAsync(taskId, request.ToInput(), cancellationToken);
            if (updated is null)
            {
                return NotFound();
            }

            return Ok(RegistrationsController.ParticipantTaskResponse.FromDomain(updated));
        }
        catch (InvalidOperationException ex)
        {
            return BuildBadRequest(ex);
        }
    }

    [HttpPatch("{taskId:guid}/status")]
    [SwaggerOperation(Summary = "تغییر وضعیت تسک", Description = "وضعیت تسک را به حالت جدید (در حال انجام، تکمیل‌شده و ... ) تغییر می‌دهد.")]
    [ProducesResponseType(typeof(RegistrationsController.ParticipantTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegistrationsController.ParticipantTaskResponse>> UpdateStatusAsync(Guid participantId, Guid taskId, [FromBody] ParticipantTaskStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _taskService.UpdateStatusAsync(taskId, request.Status, cancellationToken);
            if (updated is null)
            {
                return NotFound();
            }

            return Ok(RegistrationsController.ParticipantTaskResponse.FromDomain(updated));
        }
        catch (InvalidOperationException ex)
        {
            return BuildBadRequest(ex);
        }
    }

    [HttpDelete("{taskId:guid}")]
    [SwaggerOperation(Summary = "حذف تسک", Description = "تسک انتخاب‌شده را حذف می‌کند.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid participantId, Guid taskId, CancellationToken cancellationToken)
    {
        try
        {
            await _taskService.DeleteAsync(taskId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BuildBadRequest(ex);
        }
    }

    [HttpPost("ai-advice")]
    [SwaggerOperation(Summary = "پیشنهاد هوش مصنوعی", Description = "بر اساس زمینه وارد شده و تسک‌های موجود پیشنهادهای جدید ارائه می‌کند.")]
    [ProducesResponseType(typeof(TaskAdviceResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TaskAdviceResponse>> GenerateAdviceAsync(Guid participantId, [FromBody] ParticipantTaskAdviceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _taskService.GenerateAdviceAsync(participantId, request.ToModel(participantId), cancellationToken);
            return Ok(TaskAdviceResponse.FromDomain(result));
        }
        catch (InvalidOperationException ex)
        {
            return BuildBadRequest(ex);
        }
    }

    private BadRequestObjectResult BuildBadRequest(InvalidOperationException exception)
        => BadRequest(new ProblemDetails
        {
            Title = "امکان انجام عملیات وجود ندارد",
            Detail = exception.Message,
            Status = StatusCodes.Status400BadRequest
        });

    public record ParticipantTaskRequest
    {
        [Required]
        public Guid EventId { get; init; }
            = Guid.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; init; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; init; } = string.Empty;

        public DateTimeOffset? DueAt { get; init; }
            = null;

        [MaxLength(150)]
        public string? AssignedTo { get; init; }
            = null;

        public ParticipantTaskInput ToInput()
            => new()
            {
                EventId = EventId,
                Title = Title.Trim(),
                Description = string.IsNullOrWhiteSpace(Description) ? string.Empty : Description.Trim(),
                DueAt = DueAt,
                AssignedTo = string.IsNullOrWhiteSpace(AssignedTo) ? null : AssignedTo.Trim()
            };
    }

    public record ParticipantTaskStatusRequest
    {
        [Required]
        public ParticipantTaskStatus Status { get; init; }
            = ParticipantTaskStatus.Todo;
    }

    public record ParticipantTaskAdviceRequest
    {
        [MaxLength(6000)]
        public string? Context { get; init; }
            = null;

        [MaxLength(128)]
        public string? FocusArea { get; init; }
            = null;

        public TaskAdviceRequest ToModel(Guid participantId)
            => new()
            {
                ParticipantRegistrationId = participantId,
                EventId = Guid.Empty,
                TaskContext = Context ?? string.Empty,
                FocusArea = FocusArea,
                ExistingTasks = Array.Empty<string>()
            };
    }

    public record TaskAdviceResponse
    {
        public string Summary { get; init; } = string.Empty;
        public IReadOnlyCollection<string> SuggestedTasks { get; init; }
            = Array.Empty<string>();
        public string? Risks { get; init; }
            = null;
        public string? NextSteps { get; init; }
            = null;

        public static TaskAdviceResponse FromDomain(TaskAdviceResult result)
            => new()
            {
                Summary = result.Summary,
                SuggestedTasks = result.SuggestedTasks.ToList(),
                Risks = result.Risks,
                NextSteps = result.NextSteps
            };
    }
}
