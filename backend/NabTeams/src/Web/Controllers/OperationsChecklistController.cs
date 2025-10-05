using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Operations.Models;
using NabTeams.Domain.Enums;
using NabTeams.Web.Auth;

namespace NabTeams.Web.Controllers;

[ApiController]
[Route("api/admin/operations-checklist")]
[Authorize(Policy = AuthorizationPolicies.Admin)]
public class OperationsChecklistController : ControllerBase
{
    private readonly IOperationsChecklistService _service;

    public OperationsChecklistController(IOperationsChecklistService service)
    {
        _service = service;
    }

    [HttpGet]
    [SwaggerOperation(Summary = "فهرست آیتم‌های چک‌لیست عملیات و امنیت", Description = "لیست کامل مراحل امنیتی/عملیاتی باقی‌مانده یا تکمیل‌شده را برای ادمین بازمی‌گرداند.")]
    public async Task<ActionResult<IEnumerable<OperationsChecklistItemResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        var items = await _service.ListAsync(cancellationToken);
        return Ok(items.Select(OperationsChecklistItemResponse.FromModel));
    }

    [HttpPut("{id:guid}")]
    [SwaggerOperation(Summary = "به‌روزرسانی وضعیت یک آیتم چک‌لیست", Description = "وضعیت آیتم، یادداشت‌ها و لینک مستندات را برای پایش عملیات و امنیت تغییر می‌دهد.")]
    public async Task<ActionResult<OperationsChecklistItemResponse>> UpdateAsync(Guid id, [FromBody] OperationsChecklistUpdateRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest("بدنهٔ درخواست ارسال نشده است.");
        }

        if (!Enum.IsDefined(typeof(OperationsChecklistStatus), request.Status))
        {
            return BadRequest("وضعیت انتخاب‌شده معتبر نیست.");
        }

        try
        {
            var updated = await _service.UpdateAsync(id, new OperationsChecklistUpdateModel(request.Status, request.Notes, request.ArtifactUrl), cancellationToken);
            return Ok(OperationsChecklistItemResponse.FromModel(updated));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    public record OperationsChecklistUpdateRequest
    {
        public OperationsChecklistStatus Status { get; init; }
        public string? Notes { get; init; }
        public string? ArtifactUrl { get; init; }
    }

    public record OperationsChecklistItemResponse(
        Guid Id,
        string Key,
        string Title,
        string Description,
        string Category,
        OperationsChecklistStatus Status,
        DateTimeOffset? CompletedAt,
        string? Notes,
        string? ArtifactUrl,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt)
    {
        public static OperationsChecklistItemResponse FromModel(OperationsChecklistItemModel model) => new(
            model.Id,
            model.Key,
            model.Title,
            model.Description,
            model.Category,
            model.Status,
            model.CompletedAt,
            model.Notes,
            model.ArtifactUrl,
            model.CreatedAt,
            model.UpdatedAt);
    }
}
