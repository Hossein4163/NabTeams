using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Common;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace NabTeams.Web.Controllers;

[ApiController]
[Route("api/registrations")]
public class RegistrationsController : ControllerBase
{
    private const int MaxMembers = 10;
    private const int MaxDocuments = 10;
    private const int MaxLinks = 10;
    private const int MaxInterestAreas = 12;
    private const long MaxUploadBytes = 25 * 1024 * 1024;

    private readonly IRegistrationRepository _repository;
    private readonly IRegistrationDocumentStorage _documentStorage;
    private readonly IRegistrationWorkflowService _workflowService;

    public RegistrationsController(
        IRegistrationRepository repository,
        IRegistrationDocumentStorage documentStorage,
        IRegistrationWorkflowService workflowService)
    {
        _repository = repository;
        _documentStorage = documentStorage;
        _workflowService = workflowService;
    }

    [HttpPost("participants")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "ثبت‌نام سرپرست تیم و اعضا", Description = "درخواست اولیهٔ شرکت‌کننده را به همراه اعضای تیم، مدارک و لینک‌ها ثبت می‌کند.")]
    [ProducesResponseType(typeof(ParticipantRegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ParticipantRegistrationResponse>> CreateParticipantAsync(
        [FromBody] ParticipantRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidateParticipantRequest(request))
        {
            return ValidationProblem(ModelState);
        }

        var registration = request.ToDomain();
        var stored = await _repository.AddParticipantAsync(registration, cancellationToken);
        var response = ParticipantRegistrationResponse.FromDomain(stored);

        return CreatedAtAction(nameof(GetParticipantAsync), new { id = response.Id }, response);
    }

    [HttpPost("participants/uploads")]
    [AllowAnonymous]
    [RequestSizeLimit(MaxUploadBytes)]
    [SwaggerOperation(Summary = "آپلود فایل ثبت‌نام شرکت‌کننده", Description = "یک فایل را برای پیوست به ثبت‌نام شرکت‌کننده ذخیره می‌کند و آدرس قابل‌دسترسی آن را برمی‌گرداند.")]
    [ProducesResponseType(typeof(ParticipantDocumentUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ParticipantDocumentUploadResponse>> UploadParticipantDocumentAsync(
        [FromForm] ParticipantDocumentUploadRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            ModelState.AddModelError(nameof(request.File), "فایلی برای آپلود ارسال نشده است.");
            return ValidationProblem(ModelState);
        }

        if (request.File.Length > MaxUploadBytes)
        {
            ModelState.AddModelError(nameof(request.File), "حجم فایل انتخاب‌شده بیش از حد مجاز است (۲۵ مگابایت).");
            return ValidationProblem(ModelState);
        }

        await using var stream = request.File.OpenReadStream();
        var stored = await _documentStorage.SaveAsync(request.File.FileName, stream, cancellationToken);

        var response = new ParticipantDocumentUploadResponse
        {
            Category = request.Category,
            FileName = stored.FileName,
            FileUrl = stored.FileUrl
        };

        return Created(stored.FileUrl, response);
    }

    [HttpGet("participants")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [SwaggerOperation(Summary = "فهرست ثبت‌نام شرکت‌کنندگان", Description = "خلاصهٔ درخواست‌های ارسال‌شده توسط شرکت‌کنندگان را برای کاربر ادمین بازمی‌گرداند.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ParticipantRegistrationSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ParticipantRegistrationSummaryResponse>>> ListParticipantsAsync(CancellationToken cancellationToken)
    {
        var registrations = await _repository.ListParticipantsAsync(cancellationToken);
        var response = registrations
            .Select(ParticipantRegistrationSummaryResponse.FromDomain)
            .ToList();

        return Ok(response);
    }

    [HttpGet("participants/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [SwaggerOperation(Summary = "جزئیات ثبت‌نام شرکت‌کننده", Description = "اطلاعات کامل یک ثبت‌نام شرکت‌کننده را با شناسهٔ آن بازمی‌گرداند.")]
    [ProducesResponseType(typeof(ParticipantRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParticipantRegistrationResponse>> GetParticipantAsync(Guid id, CancellationToken cancellationToken)
    {
        var registration = await _repository.GetParticipantAsync(id, cancellationToken);
        if (registration is null)
        {
            return NotFound();
        }

        return Ok(ParticipantRegistrationResponse.FromDomain(registration));
    }

    [HttpGet("participants/{id:guid}/public")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "دریافت عمومی ثبت‌نام شرکت‌کننده", Description = "جزئیات یک ثبت‌نام شرکت‌کننده را بدون نیاز به احراز هویت برمی‌گرداند تا متقاضی بتواند آن را بازبینی کند.")]
    [ProducesResponseType(typeof(ParticipantRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParticipantRegistrationResponse>> GetParticipantPublicAsync(Guid id, CancellationToken cancellationToken)
    {
        var registration = await _repository.GetParticipantAsync(id, cancellationToken);
        if (registration is null)
        {
            return NotFound();
        }

        return Ok(ParticipantRegistrationResponse.FromDomain(registration));
    }

    [HttpPut("participants/{id:guid}")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "ویرایش ثبت‌نام شرکت‌کننده", Description = "اطلاعات ارسال‌شده توسط شرکت‌کننده را پیش از تأیید نهایی به‌روزرسانی می‌کند.")]
    [ProducesResponseType(typeof(ParticipantRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ParticipantRegistrationResponse>> UpdateParticipantAsync(
        Guid id,
        [FromBody] ParticipantRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (!ValidateParticipantRequest(request))
        {
            return ValidationProblem(ModelState);
        }

        var existing = await _repository.GetParticipantAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        if (existing.Status == RegistrationStatus.Finalized)
        {
            return Conflict(new { message = "این ثبت‌نام قبلاً نهایی شده است و قابل ویرایش نیست." });
        }

        var updatedModel = request.ToDomain(id, existing);
        var stored = await _repository.UpdateParticipantAsync(id, updatedModel, cancellationToken);
        if (stored is null)
        {
            return NotFound();
        }

        return Ok(ParticipantRegistrationResponse.FromDomain(stored));
    }

    [HttpPost("participants/{id:guid}/finalize")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "تأیید نهایی ثبت‌نام شرکت‌کننده", Description = "پس از بازبینی اطلاعات، وضعیت ثبت‌نام شرکت‌کننده را به حالت نهایی تغییر می‌دهد.")]
    [ProducesResponseType(typeof(ParticipantRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParticipantRegistrationResponse>> FinalizeParticipantAsync(
        Guid id,
        [FromBody] ParticipantFinalizeRequest? request,
        CancellationToken cancellationToken)
    {
        var stored = await _repository.FinalizeParticipantAsync(id, request?.SummaryFileUrl, cancellationToken);
        if (stored is null)
        {
            return NotFound();
        }

        return Ok(ParticipantRegistrationResponse.FromDomain(stored));
    }

    [HttpPost("participants/{id:guid}/approve")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [SwaggerOperation(Summary = "تأیید ثبت‌نام و ارسال لینک پرداخت", Description = "وضعیت ثبت‌نام شرکت‌کننده را به تایید شده تغییر می‌دهد، لینک پرداخت مرحله بعد را ایجاد می‌کند و اعلان برای سرپرست ارسال می‌شود.")]
    [ProducesResponseType(typeof(ParticipantRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParticipantRegistrationResponse>> ApproveParticipantAsync(
        Guid id,
        [FromBody] ParticipantApprovalRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            ModelState.AddModelError(nameof(request.Amount), "مبلغ پرداخت باید بزرگ‌تر از صفر باشد.");
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.Recipient))
        {
            ModelState.AddModelError(nameof(request.Recipient), "آدرس ایمیل یا شماره تماس گیرنده الزامی است.");
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.ReturnUrl))
        {
            ModelState.AddModelError(nameof(request.ReturnUrl), "آدرس بازگشت الزامی است.");
            return ValidationProblem(ModelState);
        }

        var result = await _workflowService.ApproveParticipantAsync(
            id,
            new ParticipantApprovalOptions(request.Amount, request.Currency ?? "IRR", request.Recipient, request.ReturnUrl),
            cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(ParticipantRegistrationResponse.FromDomain(result));
    }

    [HttpPost("participants/{id:guid}/payments/complete")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [SwaggerOperation(Summary = "ثبت موفقیت پرداخت شرکت‌کننده", Description = "وضعیت پرداخت ثبت‌نام شرکت‌کننده را کامل می‌کند و اعلان تأیید ارسال می‌شود.")]
    [ProducesResponseType(typeof(ParticipantRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ParticipantRegistrationResponse>> CompleteParticipantPaymentAsync(
        Guid id,
        [FromBody] ParticipantPaymentCompletionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _workflowService.CompleteParticipantPaymentAsync(id, request.GatewayReference, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        return Ok(ParticipantRegistrationResponse.FromDomain(result));
    }

    [HttpPost("participants/{id:guid}/analysis")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "تحلیل بیزینس‌پلن توسط هوش مصنوعی", Description = "Narrative طرح کسب‌وکار را به سرویس هوش مصنوعی ارسال کرده و بازخورد ساختاریافته دریافت می‌کند.")]
    [ProducesResponseType(typeof(BusinessPlanReviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessPlanReviewResponse>> AnalyzeBusinessPlanAsync(
        Guid id,
        [FromBody] BusinessPlanAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Narrative))
        {
            ModelState.AddModelError(nameof(request.Narrative), "ارائهٔ توضیحات طرح الزامی است.");
            return ValidationProblem(ModelState);
        }

        var attachmentUrls = request.AttachmentUrls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();

        var options = new BusinessPlanAnalysisOptions(
            request.Narrative.Trim(),
            attachmentUrls,
            string.IsNullOrWhiteSpace(request.AdditionalContext) ? null : request.AdditionalContext.Trim());

        var review = await _workflowService.AnalyzeBusinessPlanAsync(id, options, cancellationToken);
        if (review is null)
        {
            return NotFound();
        }

        return Ok(BusinessPlanReviewResponse.FromDomain(review));
    }

    [HttpGet("participants/{id:guid}/analysis")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "لیست تحلیل‌های بیزینس‌پلن", Description = "تمام تحلیل‌های انجام‌شده توسط هوش مصنوعی برای یک تیم را برمی‌گرداند.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<BusinessPlanReviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<BusinessPlanReviewResponse>>> ListBusinessPlanReviewsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var registration = await _repository.GetParticipantAsync(id, cancellationToken);
        if (registration is null)
        {
            return NotFound();
        }

        var reviews = await _repository.ListBusinessPlanReviewsAsync(id, cancellationToken);
        var response = reviews.Select(BusinessPlanReviewResponse.FromDomain).ToList();
        return Ok(response);
    }

    [HttpGet("participants/{id:guid}/analysis/{reviewId:guid}")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "دریافت جزئیات یک تحلیل بیزینس‌پلن", Description = "تحلیل مشخص‌شده را بر اساس شناسهٔ آن بازمی‌گرداند.")]
    [ProducesResponseType(typeof(BusinessPlanReviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessPlanReviewResponse>> GetBusinessPlanReviewAsync(
        Guid id,
        Guid reviewId,
        CancellationToken cancellationToken)
    {
        var review = await _repository.GetBusinessPlanReviewAsync(id, reviewId, cancellationToken);
        if (review is null)
        {
            return NotFound();
        }

        return Ok(BusinessPlanReviewResponse.FromDomain(review));
    }

    [HttpPost("judges")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "ثبت‌نام داور", Description = "اطلاعات هویتی و تخصصی داور را ثبت می‌کند.")]
    [ProducesResponseType(typeof(JudgeRegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<JudgeRegistrationResponse>> CreateJudgeAsync([FromBody] JudgeRegistrationRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var registration = request.ToDomain();
        var stored = await _repository.AddJudgeAsync(registration, cancellationToken);
        var response = JudgeRegistrationResponse.FromDomain(stored);
        return CreatedAtAction(nameof(GetJudgeAsync), new { id = response.Id }, response);
    }

    [HttpGet("judges")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [SwaggerOperation(Summary = "فهرست ثبت‌نام داوران", Description = "تمامی درخواست‌های داوری ثبت‌شده را برای ادمین بازمی‌گرداند.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<JudgeRegistrationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<JudgeRegistrationResponse>>> ListJudgesAsync(CancellationToken cancellationToken)
    {
        var registrations = await _repository.ListJudgesAsync(cancellationToken);
        var response = registrations
            .Select(JudgeRegistrationResponse.FromDomain)
            .ToList();

        return Ok(response);
    }

    [HttpGet("judges/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [SwaggerOperation(Summary = "جزئیات ثبت‌نام داور", Description = "اطلاعات کامل یک داور ثبت‌نام‌شده را برمی‌گرداند.")]
    [ProducesResponseType(typeof(JudgeRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JudgeRegistrationResponse>> GetJudgeAsync(Guid id, CancellationToken cancellationToken)
    {
        var registration = await _repository.GetJudgeAsync(id, cancellationToken);
        if (registration is null)
        {
            return NotFound();
        }

        return Ok(JudgeRegistrationResponse.FromDomain(registration));
    }

    [HttpPost("investors")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "ثبت‌نام سرمایه‌گذار", Description = "ورود اطلاعات هویتی سرمایه‌گذار و حوزه‌های علاقه‌مندی برای سرمایه‌گذاری.")]
    [ProducesResponseType(typeof(InvestorRegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvestorRegistrationResponse>> CreateInvestorAsync([FromBody] InvestorRegistrationRequest request, CancellationToken cancellationToken)
    {
        if (request.InterestAreas.Count > MaxInterestAreas)
        {
            ModelState.AddModelError(nameof(request.InterestAreas), $"حداکثر {MaxInterestAreas} حوزه علاقه‌مندی قابل ثبت است.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var registration = request.ToDomain();
        var stored = await _repository.AddInvestorAsync(registration, cancellationToken);
        var response = InvestorRegistrationResponse.FromDomain(stored);
        return CreatedAtAction(nameof(GetInvestorAsync), new { id = response.Id }, response);
    }

    [HttpGet("investors")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [SwaggerOperation(Summary = "فهرست ثبت‌نام سرمایه‌گذاران", Description = "لیست درخواست‌های سرمایه‌گذار ثبت‌شده را برای ادمین بازمی‌گرداند.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<InvestorRegistrationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<InvestorRegistrationResponse>>> ListInvestorsAsync(CancellationToken cancellationToken)
    {
        var registrations = await _repository.ListInvestorsAsync(cancellationToken);
        var response = registrations
            .Select(InvestorRegistrationResponse.FromDomain)
            .ToList();

        return Ok(response);
    }

    [HttpGet("investors/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    [SwaggerOperation(Summary = "جزئیات ثبت‌نام سرمایه‌گذار", Description = "اطلاعات کامل یک سرمایه‌گذار ثبت‌نام‌شده را بازمی‌گرداند.")]
    [ProducesResponseType(typeof(InvestorRegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvestorRegistrationResponse>> GetInvestorAsync(Guid id, CancellationToken cancellationToken)
    {
        var registration = await _repository.GetInvestorAsync(id, cancellationToken);
        if (registration is null)
        {
            return NotFound();
        }

        return Ok(InvestorRegistrationResponse.FromDomain(registration));
    }

    private bool ValidateParticipantRequest(ParticipantRegistrationRequest request)
    {
        if (request.Members.Count > MaxMembers)
        {
            ModelState.AddModelError(nameof(request.Members), $"حداکثر {MaxMembers} عضو تیم قابل ثبت است.");
        }

        if (request.Documents.Count > MaxDocuments)
        {
            ModelState.AddModelError(nameof(request.Documents), $"حداکثر {MaxDocuments} فایل می‌تواند بارگذاری شود.");
        }

        if (request.Links.Count > MaxLinks)
        {
            ModelState.AddModelError(nameof(request.Links), $"حداکثر {MaxLinks} لینک مجاز است.");
        }

        return ModelState.IsValid;
    }

    private static string? FormatDate(DateOnly? date)
        => date?.ToString("yyyy-MM-dd");

    public record ParticipantRegistrationRequest
    {
        [Required]
        [MaxLength(100)]
        public string HeadFirstName { get; init; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string HeadLastName { get; init; } = string.Empty;

        [Required]
        [MaxLength(16)]
        public string NationalId { get; init; } = string.Empty;

        [Required]
        [MaxLength(32)]
        public string PhoneNumber { get; init; } = string.Empty;

        [EmailAddress]
        [MaxLength(128)]
        public string? Email { get; init; }
            = null;

        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; init; }
            = null;

        [Required]
        [MaxLength(128)]
        public string EducationDegree { get; init; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string FieldOfStudy { get; init; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string TeamName { get; init; } = string.Empty;

        public bool HasTeam { get; init; } = true;

        public bool TeamCompleted { get; init; }
            = false;

        [MaxLength(1024)]
        public string? AdditionalNotes { get; init; }
            = null;

        public List<ParticipantTeamMemberRequest> Members { get; init; } = new();

        public List<ParticipantDocumentRequest> Documents { get; init; } = new();

        public List<ParticipantLinkRequest> Links { get; init; } = new();

        public ParticipantRegistration ToDomain(Guid? id = null, ParticipantRegistration? existing = null)
        {
            var normalizedId = id ?? existing?.Id ?? Guid.NewGuid();
            var status = existing?.Status ?? RegistrationStatus.Submitted;
            return new ParticipantRegistration
            {
                Id = normalizedId,
                HeadFirstName = HeadFirstName.Trim(),
                HeadLastName = HeadLastName.Trim(),
                NationalId = NationalId.Trim(),
                PhoneNumber = PhoneNumber.Trim(),
                Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                BirthDate = BirthDate.HasValue ? DateOnly.FromDateTime(BirthDate.Value.Date) : null,
                EducationDegree = EducationDegree.Trim(),
                FieldOfStudy = FieldOfStudy.Trim(),
                TeamName = TeamName.Trim(),
                HasTeam = HasTeam,
                TeamCompleted = TeamCompleted,
                AdditionalNotes = string.IsNullOrWhiteSpace(AdditionalNotes) ? null : AdditionalNotes.Trim(),
                Members = Members
                    .Select(member => member.ToDomain())
                    .ToList(),
                Documents = Documents
                    .Select(document => document.ToDomain())
                    .ToList(),
                Links = Links
                    .Select(link => link.ToDomain())
                    .ToList(),
                Status = status,
                FinalizedAt = existing?.FinalizedAt,
                SummaryFileUrl = existing?.SummaryFileUrl,
                SubmittedAt = existing?.SubmittedAt ?? DateTimeOffset.UtcNow
            };
        }
    }

    public record ParticipantTeamMemberRequest
    {
        [Required]
        [MaxLength(150)]
        public string FullName { get; init; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Role { get; init; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string FocusArea { get; init; } = string.Empty;

        public TeamMember ToDomain()
            => new()
            {
                Id = Guid.NewGuid(),
                FullName = FullName.Trim(),
                Role = Role.Trim(),
                FocusArea = FocusArea.Trim()
            };
    }

    public record ParticipantDocumentRequest
    {
        public RegistrationDocumentCategory Category { get; init; } = RegistrationDocumentCategory.ProjectArchive;

        [Required]
        [MaxLength(256)]
        public string FileName { get; init; } = string.Empty;

        [Required]
        [MaxLength(512)]
        public string FileUrl { get; init; } = string.Empty;

        public RegistrationDocument ToDomain()
            => new()
            {
                Id = Guid.NewGuid(),
                Category = Category,
                FileName = FileName.Trim(),
                FileUrl = FileUrl.Trim()
            };
    }

    public record ParticipantDocumentUploadRequest
    {
        public RegistrationDocumentCategory Category { get; init; } = RegistrationDocumentCategory.ProjectArchive;

        [Required]
        public IFormFile? File { get; init; }
            = null;
    }

    public record ParticipantDocumentUploadResponse
    {
        public RegistrationDocumentCategory Category { get; init; } = RegistrationDocumentCategory.ProjectArchive;
        public string FileName { get; init; } = string.Empty;
        public string FileUrl { get; init; } = string.Empty;
    }

    public record ParticipantLinkRequest
    {
        public RegistrationLinkType Type { get; init; } = RegistrationLinkType.Other;

        [MaxLength(128)]
        public string? Label { get; init; }
            = null;

        [Required]
        [MaxLength(512)]
        public string Url { get; init; } = string.Empty;

        public RegistrationLink ToDomain()
            => new()
            {
                Id = Guid.NewGuid(),
                Type = Type,
                Label = string.IsNullOrWhiteSpace(Label) ? Type.ToString() : Label.Trim(),
                Url = Url.Trim()
            };
    }

    public record JudgeRegistrationRequest
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; init; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; init; } = string.Empty;

        [Required]
        [MaxLength(16)]
        public string NationalId { get; init; } = string.Empty;

        [Required]
        [MaxLength(32)]
        public string PhoneNumber { get; init; } = string.Empty;

        [EmailAddress]
        [MaxLength(128)]
        public string? Email { get; init; }
            = null;

        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; init; }
            = null;

        [Required]
        [MaxLength(256)]
        public string FieldOfExpertise { get; init; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string HighestDegree { get; init; } = string.Empty;

        [MaxLength(1024)]
        public string? Biography { get; init; }
            = null;

        public JudgeRegistration ToDomain()
            => new()
            {
                Id = Guid.NewGuid(),
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                NationalId = NationalId.Trim(),
                PhoneNumber = PhoneNumber.Trim(),
                Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                BirthDate = BirthDate.HasValue ? DateOnly.FromDateTime(BirthDate.Value.Date) : null,
                FieldOfExpertise = FieldOfExpertise.Trim(),
                HighestDegree = HighestDegree.Trim(),
                Biography = string.IsNullOrWhiteSpace(Biography) ? null : Biography.Trim(),
                SubmittedAt = DateTimeOffset.UtcNow
            };
    }

    public record InvestorRegistrationRequest
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; init; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; init; } = string.Empty;

        [Required]
        [MaxLength(16)]
        public string NationalId { get; init; } = string.Empty;

        [Required]
        [MaxLength(32)]
        public string PhoneNumber { get; init; } = string.Empty;

        [EmailAddress]
        [MaxLength(128)]
        public string? Email { get; init; }
            = null;

        [MaxLength(1024)]
        public string? AdditionalNotes { get; init; }
            = null;

        public List<string> InterestAreas { get; init; } = new();

        public InvestorRegistration ToDomain()
            => new()
            {
                Id = Guid.NewGuid(),
                FirstName = FirstName.Trim(),
                LastName = LastName.Trim(),
                NationalId = NationalId.Trim(),
                PhoneNumber = PhoneNumber.Trim(),
                Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                AdditionalNotes = string.IsNullOrWhiteSpace(AdditionalNotes) ? null : AdditionalNotes.Trim(),
                InterestAreas = InterestAreas
                    .Where(area => !string.IsNullOrWhiteSpace(area))
                    .Select(area => area.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(MaxInterestAreas)
                    .ToList(),
                SubmittedAt = DateTimeOffset.UtcNow
            };
    }

    public record ParticipantFinalizeRequest
    {
        [MaxLength(512)]
        public string? SummaryFileUrl { get; init; }
            = null;
    }

    public record ParticipantApprovalRequest
    {
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; init; }
            = 0m;

        [MaxLength(16)]
        public string? Currency { get; init; }
            = "IRR";

        [Required]
        [MaxLength(256)]
        public string Recipient { get; init; } = string.Empty;

        [Required]
        [MaxLength(512)]
        [Url]
        public string ReturnUrl { get; init; } = string.Empty;
    }

    public record ParticipantPaymentCompletionRequest
    {
        [MaxLength(128)]
        public string? GatewayReference { get; init; }
            = null;
    }

    public record BusinessPlanAnalysisRequest
    {
        [Required]
        [MaxLength(12000)]
        public string Narrative { get; init; } = string.Empty;

        [MaxLength(2000)]
        public string? AdditionalContext { get; init; }
            = null;

        public IList<string> AttachmentUrls { get; init; } = new List<string>();
    }

    public record ParticipantRegistrationSummaryResponse
    {
        public Guid Id { get; init; }
        public string HeadFullName { get; init; } = string.Empty;
        public string TeamName { get; init; } = string.Empty;
        public bool TeamCompleted { get; init; }
            = false;
        public RegistrationStatus Status { get; init; }
            = RegistrationStatus.Submitted;
        public DateTimeOffset? FinalizedAt { get; init; }
            = null;
        public DateTimeOffset SubmittedAt { get; init; }
            = DateTimeOffset.UtcNow;
        public decimal? LatestBusinessPlanScore { get; init; }
            = null;

        public static ParticipantRegistrationSummaryResponse FromDomain(ParticipantRegistration registration)
            => new()
            {
                Id = registration.Id,
                HeadFullName = $"{registration.HeadFirstName} {registration.HeadLastName}".Trim(),
                TeamName = registration.TeamName,
                TeamCompleted = registration.TeamCompleted,
                Status = registration.Status,
                FinalizedAt = registration.FinalizedAt,
                SubmittedAt = registration.SubmittedAt,
                LatestBusinessPlanScore = registration.BusinessPlanReviews.FirstOrDefault()?.OverallScore
            };
    }

    public record ParticipantRegistrationResponse
    {
        public Guid Id { get; init; }
        public string HeadFirstName { get; init; } = string.Empty;
        public string HeadLastName { get; init; } = string.Empty;
        public string NationalId { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public string? Email { get; init; }
            = null;
        public string? BirthDate { get; init; }
            = null;
        public string EducationDegree { get; init; } = string.Empty;
        public string FieldOfStudy { get; init; } = string.Empty;
        public string TeamName { get; init; } = string.Empty;
        public bool HasTeam { get; init; }
            = true;
        public bool TeamCompleted { get; init; }
            = false;
        public string? AdditionalNotes { get; init; }
            = null;
        public RegistrationStatus Status { get; init; }
            = RegistrationStatus.Submitted;
        public DateTimeOffset? FinalizedAt { get; init; }
            = null;
        public string? SummaryFileUrl { get; init; }
            = null;
        public DateTimeOffset SubmittedAt { get; init; }
            = DateTimeOffset.UtcNow;
        public IReadOnlyCollection<ParticipantTeamMemberResponse> Members { get; init; }
            = Array.Empty<ParticipantTeamMemberResponse>();
        public IReadOnlyCollection<ParticipantDocumentResponse> Documents { get; init; }
            = Array.Empty<ParticipantDocumentResponse>();
        public IReadOnlyCollection<ParticipantLinkResponse> Links { get; init; }
            = Array.Empty<ParticipantLinkResponse>();
        public ParticipantPaymentResponse? Payment { get; init; }
            = null;
        public IReadOnlyCollection<ParticipantNotificationResponse> Notifications { get; init; }
            = Array.Empty<ParticipantNotificationResponse>();
        public IReadOnlyCollection<BusinessPlanReviewResponse> BusinessPlanReviews { get; init; }
            = Array.Empty<BusinessPlanReviewResponse>();

        public static ParticipantRegistrationResponse FromDomain(ParticipantRegistration registration)
            => new()
            {
                Id = registration.Id,
                HeadFirstName = registration.HeadFirstName,
                HeadLastName = registration.HeadLastName,
                NationalId = registration.NationalId,
                PhoneNumber = registration.PhoneNumber,
                Email = registration.Email,
                BirthDate = FormatDate(registration.BirthDate),
                EducationDegree = registration.EducationDegree,
                FieldOfStudy = registration.FieldOfStudy,
                TeamName = registration.TeamName,
                HasTeam = registration.HasTeam,
                TeamCompleted = registration.TeamCompleted,
                AdditionalNotes = registration.AdditionalNotes,
                Status = registration.Status,
                FinalizedAt = registration.FinalizedAt,
                SummaryFileUrl = registration.SummaryFileUrl,
                SubmittedAt = registration.SubmittedAt,
                Members = registration.Members
                    .Select(ParticipantTeamMemberResponse.FromDomain)
                    .ToList(),
                Documents = registration.Documents
                    .Select(ParticipantDocumentResponse.FromDomain)
                    .ToList(),
                Links = registration.Links
                    .Select(ParticipantLinkResponse.FromDomain)
                    .ToList(),
                Payment = registration.Payment is null
                    ? null
                    : ParticipantPaymentResponse.FromDomain(registration.Payment),
                Notifications = registration.Notifications
                    .Select(ParticipantNotificationResponse.FromDomain)
                    .ToList(),
                BusinessPlanReviews = registration.BusinessPlanReviews
                    .Select(BusinessPlanReviewResponse.FromDomain)
                    .ToList()
            };
    }

    public record ParticipantTeamMemberResponse
    {
        public Guid Id { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public string FocusArea { get; init; } = string.Empty;

        public static ParticipantTeamMemberResponse FromDomain(TeamMember member)
            => new()
            {
                Id = member.Id,
                FullName = member.FullName,
                Role = member.Role,
                FocusArea = member.FocusArea
            };
    }

    public record ParticipantDocumentResponse
    {
        public Guid Id { get; init; }
        public RegistrationDocumentCategory Category { get; init; }
            = RegistrationDocumentCategory.ProjectArchive;
        public string FileName { get; init; } = string.Empty;
        public string FileUrl { get; init; } = string.Empty;

        public static ParticipantDocumentResponse FromDomain(RegistrationDocument document)
            => new()
            {
                Id = document.Id,
                Category = document.Category,
                FileName = document.FileName,
                FileUrl = document.FileUrl
            };
    }

    public record ParticipantLinkResponse
    {
        public Guid Id { get; init; }
        public RegistrationLinkType Type { get; init; }
            = RegistrationLinkType.Other;
        public string Label { get; init; } = string.Empty;
        public string Url { get; init; } = string.Empty;

        public static ParticipantLinkResponse FromDomain(RegistrationLink link)
            => new()
            {
                Id = link.Id,
                Type = link.Type,
                Label = link.Label,
                Url = link.Url
            };
    }

    public record ParticipantPaymentResponse
    {
        public Guid Id { get; init; }
        public decimal Amount { get; init; }
        public string Currency { get; init; } = string.Empty;
        public string PaymentUrl { get; init; } = string.Empty;
        public RegistrationPaymentStatus Status { get; init; }
            = RegistrationPaymentStatus.Pending;
        public DateTimeOffset RequestedAt { get; init; }
            = DateTimeOffset.UtcNow;
        public DateTimeOffset? CompletedAt { get; init; }
            = null;
        public string? GatewayReference { get; init; }
            = null;

        public static ParticipantPaymentResponse FromDomain(RegistrationPayment payment)
            => new()
            {
                Id = payment.Id,
                Amount = payment.Amount,
                Currency = payment.Currency,
                PaymentUrl = payment.PaymentUrl,
                Status = payment.Status,
                RequestedAt = payment.RequestedAt,
                CompletedAt = payment.CompletedAt,
                GatewayReference = payment.GatewayReference
            };
    }

    public record ParticipantNotificationResponse
    {
        public Guid Id { get; init; }
        public NotificationChannel Channel { get; init; }
            = NotificationChannel.Email;
        public string Recipient { get; init; } = string.Empty;
        public string Subject { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public DateTimeOffset SentAt { get; init; }
            = DateTimeOffset.UtcNow;

        public static ParticipantNotificationResponse FromDomain(RegistrationNotification notification)
            => new()
            {
                Id = notification.Id,
                Channel = notification.Channel,
                Recipient = notification.Recipient,
                Subject = notification.Subject,
                Message = notification.Message,
                SentAt = notification.SentAt
            };
    }

    public record BusinessPlanReviewResponse
    {
        public Guid Id { get; init; }
        public BusinessPlanReviewStatus Status { get; init; } = BusinessPlanReviewStatus.Completed;
        public decimal? OverallScore { get; init; }
            = null;
        public string Summary { get; init; } = string.Empty;
        public string Strengths { get; init; } = string.Empty;
        public string Risks { get; init; } = string.Empty;
        public string Recommendations { get; init; } = string.Empty;
        public string? SourceDocumentUrl { get; init; }
            = null;
        public string Model { get; init; } = string.Empty;
        public DateTimeOffset CreatedAt { get; init; }
            = DateTimeOffset.UtcNow;

        public static BusinessPlanReviewResponse FromDomain(BusinessPlanReview review)
            => new()
            {
                Id = review.Id,
                Status = review.Status,
                OverallScore = review.OverallScore,
                Summary = review.Summary,
                Strengths = review.Strengths,
                Risks = review.Risks,
                Recommendations = review.Recommendations,
                SourceDocumentUrl = review.SourceDocumentUrl,
                Model = review.Model,
                CreatedAt = review.CreatedAt
            };
    }

    public record JudgeRegistrationResponse
    {
        public Guid Id { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string NationalId { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public string? Email { get; init; }
            = null;
        public string? BirthDate { get; init; }
            = null;
        public string FieldOfExpertise { get; init; } = string.Empty;
        public string HighestDegree { get; init; } = string.Empty;
        public string? Biography { get; init; }
            = null;
        public RegistrationStatus Status { get; init; }
            = RegistrationStatus.Submitted;
        public DateTimeOffset? FinalizedAt { get; init; }
            = null;
        public DateTimeOffset SubmittedAt { get; init; }
            = DateTimeOffset.UtcNow;

        public static JudgeRegistrationResponse FromDomain(JudgeRegistration registration)
            => new()
            {
                Id = registration.Id,
                FirstName = registration.FirstName,
                LastName = registration.LastName,
                NationalId = registration.NationalId,
                PhoneNumber = registration.PhoneNumber,
                Email = registration.Email,
                BirthDate = FormatDate(registration.BirthDate),
                FieldOfExpertise = registration.FieldOfExpertise,
                HighestDegree = registration.HighestDegree,
                Biography = registration.Biography,
                Status = registration.Status,
                FinalizedAt = registration.FinalizedAt,
                SubmittedAt = registration.SubmittedAt
            };
    }

    public record InvestorRegistrationResponse
    {
        public Guid Id { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string NationalId { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public string? Email { get; init; }
            = null;
        public IReadOnlyCollection<string> InterestAreas { get; init; }
            = Array.Empty<string>();
        public string? AdditionalNotes { get; init; }
            = null;
        public RegistrationStatus Status { get; init; }
            = RegistrationStatus.Submitted;
        public DateTimeOffset? FinalizedAt { get; init; }
            = null;
        public DateTimeOffset SubmittedAt { get; init; }
            = DateTimeOffset.UtcNow;

        public static InvestorRegistrationResponse FromDomain(InvestorRegistration registration)
            => new()
            {
                Id = registration.Id,
                FirstName = registration.FirstName,
                LastName = registration.LastName,
                NationalId = registration.NationalId,
                PhoneNumber = registration.PhoneNumber,
                Email = registration.Email,
                InterestAreas = registration.InterestAreas.ToList(),
                AdditionalNotes = registration.AdditionalNotes,
                Status = registration.Status,
                FinalizedAt = registration.FinalizedAt,
                SubmittedAt = registration.SubmittedAt
            };
    }
}
