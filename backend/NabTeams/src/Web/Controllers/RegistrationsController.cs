using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Common;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;

namespace NabTeams.Web.Controllers;

[ApiController]
[Route("api/registrations")]
public class RegistrationsController : ControllerBase
{
    private const int MaxMembers = 10;
    private const int MaxDocuments = 10;
    private const int MaxLinks = 10;
    private const int MaxInterestAreas = 12;

    private readonly IRegistrationRepository _repository;

    public RegistrationsController(IRegistrationRepository repository)
    {
        _repository = repository;
    }

    [HttpPost("participants")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ParticipantRegistrationResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<ParticipantRegistrationResponse>> CreateParticipantAsync(
        [FromBody] ParticipantRegistrationRequest request,
        CancellationToken cancellationToken)
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

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var registration = request.ToDomain();
        var stored = await _repository.AddParticipantAsync(registration, cancellationToken);
        var response = ParticipantRegistrationResponse.FromDomain(stored);

        return CreatedAtAction(nameof(GetParticipantAsync), new { id = response.Id }, response);
    }

    [HttpGet("participants")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
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

    [HttpPost("judges")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(JudgeRegistrationResponse), StatusCodes.Status201Created)]
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
    [ProducesResponseType(typeof(InvestorRegistrationResponse), StatusCodes.Status201Created)]
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

        public ParticipantRegistration ToDomain()
        {
            return new ParticipantRegistration
            {
                Id = Guid.NewGuid(),
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
                SubmittedAt = DateTimeOffset.UtcNow
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

    public record ParticipantRegistrationSummaryResponse
    {
        public Guid Id { get; init; }
        public string HeadFullName { get; init; } = string.Empty;
        public string TeamName { get; init; } = string.Empty;
        public bool TeamCompleted { get; init; }
            = false;
        public DateTimeOffset SubmittedAt { get; init; }
            = DateTimeOffset.UtcNow;

        public static ParticipantRegistrationSummaryResponse FromDomain(ParticipantRegistration registration)
            => new()
            {
                Id = registration.Id,
                HeadFullName = $"{registration.HeadFirstName} {registration.HeadLastName}".Trim(),
                TeamName = registration.TeamName,
                TeamCompleted = registration.TeamCompleted,
                SubmittedAt = registration.SubmittedAt
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
        public DateTimeOffset SubmittedAt { get; init; }
            = DateTimeOffset.UtcNow;
        public IReadOnlyCollection<ParticipantTeamMemberResponse> Members { get; init; }
            = Array.Empty<ParticipantTeamMemberResponse>();
        public IReadOnlyCollection<ParticipantDocumentResponse> Documents { get; init; }
            = Array.Empty<ParticipantDocumentResponse>();
        public IReadOnlyCollection<ParticipantLinkResponse> Links { get; init; }
            = Array.Empty<ParticipantLinkResponse>();

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
                SubmittedAt = registration.SubmittedAt,
                Members = registration.Members
                    .Select(ParticipantTeamMemberResponse.FromDomain)
                    .ToList(),
                Documents = registration.Documents
                    .Select(ParticipantDocumentResponse.FromDomain)
                    .ToList(),
                Links = registration.Links
                    .Select(ParticipantLinkResponse.FromDomain)
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
                SubmittedAt = registration.SubmittedAt
            };
    }
}
