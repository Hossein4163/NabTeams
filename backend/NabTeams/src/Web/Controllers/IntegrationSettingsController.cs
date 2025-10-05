using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using NabTeams.Web.Configuration;
using Swashbuckle.AspNetCore.Annotations;

namespace NabTeams.Web.Controllers;

[ApiController]
[Route("api/admin/integrations")]
[Authorize(Policy = AuthorizationPolicies.Admin)]
public class IntegrationSettingsController : ControllerBase
{
    private readonly IIntegrationSettingsService _service;
    private readonly IAuditLogService _auditLogService;

    public IntegrationSettingsController(IIntegrationSettingsService service, IAuditLogService auditLogService)
    {
        _service = service;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [SwaggerOperation(Summary = "دریافت فهرست تنظیمات یکپارچه‌سازی", Description = "تنظیمات ثبت‌شده برای کلیدها و درگاه‌های خارجی را بازیابی می‌کند.")]
    [ProducesResponseType(typeof(IEnumerable<IntegrationSettingResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<IntegrationSettingResponse>>> ListAsync([FromQuery] string? type, CancellationToken cancellationToken)
    {
        var providerType = ParseType(type, allowNull: true);
        if (providerType is null && !string.IsNullOrWhiteSpace(type))
        {
            return BadRequest(new { message = "نوع یکپارچه‌سازی معتبر نیست." });
        }

        var items = await _service.ListAsync(providerType, cancellationToken);
        return Ok(items.Select(IntegrationSettingResponse.FromModel));
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "مشاهده جزئیات تنظیمات", Description = "یک رکورد تنظیمات یکپارچه‌سازی را بر اساس شناسه باز می‌گرداند.")]
    [ProducesResponseType(typeof(IntegrationSettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IntegrationSettingResponse>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var setting = await _service.GetAsync(id, cancellationToken);
        if (setting is null)
        {
            return NotFound();
        }

        return Ok(IntegrationSettingResponse.FromModel(setting));
    }

    [HttpPost]
    [SwaggerOperation(Summary = "ایجاد یا ویرایش تنظیمات", Description = "به ادمین اجازه می‌دهد مشخصات یکپارچه‌سازی را در پایگاه‌داده ذخیره یا ویرایش کند.")]
    [ProducesResponseType(typeof(IntegrationSettingResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<IntegrationSettingResponse>> UpsertAsync([FromBody] IntegrationSettingUpsertRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderKey))
        {
            ModelState.AddModelError(nameof(request.ProviderKey), "کلید ارائه‌دهنده الزامی است.");
        }

        var providerType = ParseType(request.Type, allowNull: false);
        if (providerType is null)
        {
            ModelState.AddModelError(nameof(request.Type), "نوع یکپارچه‌سازی معتبر نیست.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        IntegrationSetting? existing = null;
        if (request.Id is Guid identifier)
        {
            existing = await _service.GetAsync(identifier, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        var trimmedProvider = request.ProviderKey.Trim();
        var trimmedDisplay = string.IsNullOrWhiteSpace(request.DisplayName)
            ? trimmedProvider
            : request.DisplayName!.Trim();

        var model = existing is not null
            ? existing with
            {
                Type = providerType!.Value,
                ProviderKey = trimmedProvider,
                DisplayName = trimmedDisplay,
                Configuration = request.Configuration ?? string.Empty,
                UpdatedAt = now
            }
            : new IntegrationSetting
            {
                Id = request.Id ?? Guid.Empty,
                Type = providerType!.Value,
                ProviderKey = trimmedProvider,
                DisplayName = trimmedDisplay,
                Configuration = request.Configuration ?? string.Empty,
                IsActive = false,
                CreatedAt = now,
                UpdatedAt = now
            };

        var saved = await _service.UpsertAsync(model, request.Activate, cancellationToken);

        var (actorId, actorName) = ResolveActor();
        await _auditLogService.LogAsync(
            actorId,
            actorName,
            existing is null ? "IntegrationSettings.Create" : "IntegrationSettings.Update",
            nameof(IntegrationSetting),
            saved.Id.ToString(),
            new
            {
                Type = saved.Type.ToString(),
                saved.ProviderKey,
                saved.IsActive,
                Activate = request.Activate
            },
            cancellationToken);

        return Ok(IntegrationSettingResponse.FromModel(saved));
    }

    [HttpPost("{id:guid}/activate")]
    [SwaggerOperation(Summary = "فعال‌سازی تنظیمات", Description = "یک رکورد را به‌عنوان تنظیم فعال برای نوع مشخص‌شده انتخاب می‌کند.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ActivateAsync(Guid id, CancellationToken cancellationToken)
    {
        await _service.SetActiveAsync(id, cancellationToken);

        var (actorId, actorName) = ResolveActor();
        await _auditLogService.LogAsync(
            actorId,
            actorName,
            "IntegrationSettings.Activate",
            nameof(IntegrationSetting),
            id.ToString(),
            null,
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "حذف تنظیمات", Description = "رکورد تنظیمات مشخص‌شده را حذف می‌کند.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);

        var (actorId, actorName) = ResolveActor();
        await _auditLogService.LogAsync(
            actorId,
            actorName,
            "IntegrationSettings.Delete",
            nameof(IntegrationSetting),
            id.ToString(),
            null,
            cancellationToken);
        return NoContent();
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

    private static IntegrationProviderType? ParseType(string? type, bool allowNull)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return allowNull ? null : (IntegrationProviderType?)null;
        }

        return Enum.TryParse<IntegrationProviderType>(type, true, out var parsed)
            ? parsed
            : null;
    }

    public sealed class IntegrationSettingUpsertRequest
    {
        public Guid? Id { get; init; }

        [Required]
        public string Type { get; init; } = string.Empty;

        [Required]
        public string ProviderKey { get; init; } = string.Empty;

        public string? DisplayName { get; init; }

        public string? Configuration { get; init; }

        public bool Activate { get; init; }
    }

    public sealed record IntegrationSettingResponse
    (
        Guid Id,
        string Type,
        string ProviderKey,
        string DisplayName,
        bool IsActive,
        string Configuration,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt)
    {
        public static IntegrationSettingResponse FromModel(IntegrationSetting setting)
            => new(
                setting.Id,
                setting.Type.ToString(),
                setting.ProviderKey,
                setting.DisplayName,
                setting.IsActive,
                setting.Configuration,
                setting.CreatedAt,
                setting.UpdatedAt);
    }
}
