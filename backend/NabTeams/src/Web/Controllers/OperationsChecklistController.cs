using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Operations.Models;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using NabTeams.Web.Auth;

namespace NabTeams.Web.Controllers;

[ApiController]
[Route("api/admin/operations-checklist")]
[Authorize(Policy = AuthorizationPolicies.Admin)]
public class OperationsChecklistController : ControllerBase
{
    private readonly IOperationsChecklistService _service;
    private readonly IAuditLogService _auditLogService;
    private readonly IOperationsArtifactStorage _artifactStorage;

    public OperationsChecklistController(
        IOperationsChecklistService service,
        IAuditLogService auditLogService,
        IOperationsArtifactStorage artifactStorage)
    {
        _service = service;
        _auditLogService = auditLogService;
        _artifactStorage = artifactStorage;
    }

    [HttpGet]
    [SwaggerOperation(Summary = "فهرست آیتم‌های چک‌لیست عملیات و امنیت", Description = "لیست کامل مراحل امنیتی/عملیاتی باقی‌مانده یا تکمیل‌شده را برای ادمین بازمی‌گرداند.")]
    public async Task<ActionResult<IEnumerable<OperationsChecklistItemResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        var items = await _service.ListAsync(cancellationToken);
        return Ok(items.Select(OperationsChecklistItemResponse.FromModel));
    }

    [HttpPost("{id:guid}/artifact")]
    [RequestFormLimits(MultipartBodyLengthLimit = 20 * 1024 * 1024)]
    [SwaggerOperation(
        Summary = "آپلود مستند برای آیتم عملیات",
        Description = "گزارش یا فایل تکمیلی مرتبط با آیتم را بارگذاری می‌کند و لینک آن را در چک‌لیست ذخیره می‌سازد.")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<OperationsChecklistItemResponse>> UploadArtifactAsync(
        Guid id,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("فایل معتبری ارسال نشده است.");
        }

        OperationsChecklistItemModel item;
        try
        {
            item = await _service.GetAsync(id, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        await using var stream = file.OpenReadStream();
        var stored = await _artifactStorage.SaveAsync(item.Key, file.FileName, stream, cancellationToken);

        var updated = await _service.UpdateAsync(
            id,
            new OperationsChecklistUpdateModel(item.Status, item.Notes, stored.FileUrl),
            cancellationToken);

        var (actorId, actorName) = ResolveActor();
        await _auditLogService.LogAsync(
            actorId,
            actorName,
            "OperationsChecklist.UploadArtifact",
            nameof(OperationsChecklistItemEntity),
            updated.Id.ToString(),
            new
            {
                updated.ArtifactUrl,
                file.FileName
            },
            cancellationToken);

        return Ok(OperationsChecklistItemResponse.FromModel(updated));
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

            var (actorId, actorName) = ResolveActor();
            await _auditLogService.LogAsync(
                actorId,
                actorName,
                "OperationsChecklist.Update",
                nameof(OperationsChecklistItemEntity),
                updated.Id.ToString(),
            new
            {
                Status = updated.Status.ToString(),
                updated.ArtifactUrl,
                updated.Notes
            },
                cancellationToken);

            return Ok(OperationsChecklistItemResponse.FromModel(updated));
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    private (string Id, string Name) ResolveActor()
    {
        var id = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User?.FindFirstValue("sub")
                 ?? HttpContext?.User?.Identity?.Name
                 ?? "system";
        var name = User?.Identity?.Name
                   ?? User?.FindFirstValue("name")
                   ?? id;
        return (id, name);
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
