using System;
using System.Collections.Generic;
using NabTeams.Domain.Enums;

namespace NabTeams.Domain.Entities;

public record ParticipantRegistration
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string HeadFirstName { get; init; } = string.Empty;
    public string HeadLastName { get; init; } = string.Empty;
    public string NationalId { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
        = null;
    public DateOnly? BirthDate { get; init; }
        = null;
    public string EducationDegree { get; init; } = string.Empty;
    public string FieldOfStudy { get; init; } = string.Empty;
    public string TeamName { get; init; } = string.Empty;
    public bool HasTeam { get; init; }
        = true;
    public bool TeamCompleted { get; init; }
        = false;
    public IReadOnlyCollection<TeamMember> Members { get; init; }
        = Array.Empty<TeamMember>();
    public IReadOnlyCollection<RegistrationDocument> Documents { get; init; }
        = Array.Empty<RegistrationDocument>();
    public IReadOnlyCollection<RegistrationLink> Links { get; init; }
        = Array.Empty<RegistrationLink>();
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
}

public record TeamMember
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string FocusArea { get; init; } = string.Empty;
}

public record RegistrationDocument
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public RegistrationDocumentCategory Category { get; init; }
        = RegistrationDocumentCategory.ProjectArchive;
    public string FileName { get; init; } = string.Empty;
    public string FileUrl { get; init; } = string.Empty;
}

public record RegistrationLink
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public RegistrationLinkType Type { get; init; }
        = RegistrationLinkType.Other;
    public string Label { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
}

public record JudgeRegistration
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string NationalId { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
        = null;
    public DateOnly? BirthDate { get; init; }
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
}

public record InvestorRegistration
{
    public Guid Id { get; init; } = Guid.NewGuid();
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
}
