using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using NabTeams.Web.Controllers;
using Xunit;

namespace NabTeams.Api.Tests;

public class RegistrationsControllerTests
{
    private static RegistrationsController CreateController(
        Mock<IRegistrationRepository> repository,
        Mock<IRegistrationDocumentStorage>? storage = null,
        Mock<IRegistrationWorkflowService>? workflow = null,
        Mock<IRegistrationSummaryBuilder>? summaryBuilder = null)
    {
        storage ??= new Mock<IRegistrationDocumentStorage>();
        workflow ??= new Mock<IRegistrationWorkflowService>();
        summaryBuilder ??= new Mock<IRegistrationSummaryBuilder>();
        return new RegistrationsController(repository.Object, storage.Object, workflow.Object, summaryBuilder.Object);
    }

    [Fact]
    public async Task CreateParticipantAsync_ReturnsValidationProblem_WhenMembersExceedLimit()
    {
        var repository = new Mock<IRegistrationRepository>(MockBehavior.Strict);
        var controller = CreateController(repository);

        var members = Enumerable.Range(0, 11)
            .Select(i => new RegistrationsController.ParticipantTeamMemberRequest
            {
                FullName = $"Member {i}",
                Role = "Developer",
                FocusArea = "Frontend"
            })
            .ToList();

        var request = new RegistrationsController.ParticipantRegistrationRequest
        {
            HeadFirstName = "Ali",
            HeadLastName = "Rezai",
            NationalId = "1234567890",
            PhoneNumber = "09120000000",
            EducationDegree = "Bachelor",
            FieldOfStudy = "Software",
            TeamName = "Dream Team",
            HasTeam = true,
            Members = members
        };

        var result = await controller.CreateParticipantAsync(request, CancellationToken.None);

        var validationResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, validationResult.StatusCode);
        var problem = Assert.IsType<ValidationProblemDetails>(validationResult.Value);
        Assert.Contains(nameof(request.Members), problem.Errors.Keys);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateParticipantAsync_ReturnsCreated_WhenPayloadValid()
    {
        var repository = new Mock<IRegistrationRepository>();
        repository.Setup(r => r.AddParticipantAsync(It.IsAny<ParticipantRegistration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantRegistration registration, CancellationToken _) => registration);

        var controller = CreateController(repository);

        var request = new RegistrationsController.ParticipantRegistrationRequest
        {
            HeadFirstName = "Sara",
            HeadLastName = "Karimi",
            NationalId = "9876543210",
            PhoneNumber = "09350000000",
            EducationDegree = "Master",
            FieldOfStudy = "AI",
            TeamName = "Innovators",
            HasTeam = true,
            TeamCompleted = true,
            Members =
            {
                new RegistrationsController.ParticipantTeamMemberRequest
                {
                    FullName = "Reza Omidi",
                    Role = "Backend",
                    FocusArea = "APIs"
                }
            },
            Documents =
            {
                new RegistrationsController.ParticipantDocumentRequest
                {
                    Category = RegistrationDocumentCategory.ProjectArchive,
                    FileName = "proposal.pdf",
                    FileUrl = "https://files.example.com/proposal.pdf"
                }
            },
            Links =
            {
                new RegistrationsController.ParticipantLinkRequest
                {
                    Type = RegistrationLinkType.LinkedIn,
                    Label = "LinkedIn",
                    Url = "https://linkedin.com/in/innovators"
                }
            }
        };

        var result = await controller.CreateParticipantAsync(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(RegistrationsController.GetParticipantAsync), created.ActionName);
        var payload = Assert.IsType<RegistrationsController.ParticipantRegistrationResponse>(created.Value);
        Assert.Equal(request.TeamName, payload.TeamName);

        repository.Verify(r => r.AddParticipantAsync(It.IsAny<ParticipantRegistration>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateInvestorAsync_ReturnsValidationProblem_WhenInterestAreasTooMany()
    {
        var repository = new Mock<IRegistrationRepository>(MockBehavior.Strict);
        var controller = CreateController(repository);

        var request = new RegistrationsController.InvestorRegistrationRequest
        {
            FirstName = "Nima",
            LastName = "Ahmadi",
            NationalId = "1357924680",
            PhoneNumber = "09121112222",
            InterestAreas = Enumerable.Range(0, 13)
                .Select(i => $"Area {i}")
                .ToList()
        };

        var result = await controller.CreateInvestorAsync(request, CancellationToken.None);

        var validationResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, validationResult.StatusCode);
        var problem = Assert.IsType<ValidationProblemDetails>(validationResult.Value);
        Assert.Contains(nameof(request.InterestAreas), problem.Errors.Keys);
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetParticipantAsync_ReturnsNotFound_WhenMissing()
    {
        var repository = new Mock<IRegistrationRepository>();
        repository.Setup(r => r.GetParticipantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantRegistration?)null);

        var controller = CreateController(repository);

        var result = await controller.GetParticipantAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task FinalizeParticipantAsync_UsesExistingSummary_WhenAvailable()
    {
        var repository = new Mock<IRegistrationRepository>();
        var summaryBuilder = new Mock<IRegistrationSummaryBuilder>(MockBehavior.Strict);
        var registrationId = Guid.NewGuid();
        var existing = new ParticipantRegistration
        {
            Id = registrationId,
            HeadFirstName = "Arman",
            HeadLastName = "Nazari",
            NationalId = "1234567890",
            PhoneNumber = "09120000000",
            EducationDegree = "Bachelor",
            FieldOfStudy = "Computer Science",
            TeamName = "AI Builders",
            HasTeam = true,
            TeamCompleted = true,
            Status = RegistrationStatus.Submitted,
            SummaryFileUrl = "https://files.example.com/summary.txt",
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };
        var finalized = existing with
        {
            Status = RegistrationStatus.Finalized,
            FinalizedAt = DateTimeOffset.UtcNow
        };

        repository
            .Setup(r => r.GetParticipantAsync(registrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        repository
            .Setup(r => r.FinalizeParticipantAsync(registrationId, existing.SummaryFileUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(finalized);

        var controller = CreateController(repository, summaryBuilder: summaryBuilder);

        var result = await controller.FinalizeParticipantAsync(registrationId, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<RegistrationsController.ParticipantRegistrationResponse>(ok.Value);
        Assert.Equal(existing.SummaryFileUrl, payload.SummaryFileUrl);
        repository.Verify(r => r.FinalizeParticipantAsync(registrationId, existing.SummaryFileUrl, It.IsAny<CancellationToken>()), Times.Once);
        summaryBuilder.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FinalizeParticipantAsync_GeneratesSummary_WhenMissing()
    {
        var repository = new Mock<IRegistrationRepository>();
        var summaryBuilder = new Mock<IRegistrationSummaryBuilder>();
        var registrationId = Guid.NewGuid();
        var existing = new ParticipantRegistration
        {
            Id = registrationId,
            HeadFirstName = "Neda",
            HeadLastName = "Moradi",
            NationalId = "1111111111",
            PhoneNumber = "09121112223",
            EducationDegree = "Master",
            FieldOfStudy = "AI",
            TeamName = "Visionaries",
            HasTeam = true,
            TeamCompleted = true,
            Status = RegistrationStatus.Submitted,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        var generated = new StoredRegistrationDocument("visionaries-summary.txt", "https://files.example.com/visionaries-summary.txt");
        var finalized = existing with
        {
            Status = RegistrationStatus.Finalized,
            FinalizedAt = DateTimeOffset.UtcNow,
            SummaryFileUrl = generated.FileUrl
        };

        repository
            .Setup(r => r.GetParticipantAsync(registrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        summaryBuilder
            .Setup(s => s.BuildSummaryAsync(existing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(generated);
        repository
            .Setup(r => r.FinalizeParticipantAsync(registrationId, generated.FileUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(finalized);

        var controller = CreateController(repository, summaryBuilder: summaryBuilder);

        var result = await controller.FinalizeParticipantAsync(registrationId, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<RegistrationsController.ParticipantRegistrationResponse>(ok.Value);
        Assert.Equal(generated.FileUrl, payload.SummaryFileUrl);
        summaryBuilder.Verify(s => s.BuildSummaryAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(r => r.FinalizeParticipantAsync(registrationId, generated.FileUrl, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveParticipantAsync_ReturnsValidationProblem_WhenAmountInvalid()
    {
        var repository = new Mock<IRegistrationRepository>();
        var workflow = new Mock<IRegistrationWorkflowService>(MockBehavior.Strict);
        var controller = CreateController(repository, workflow: workflow);

        var request = new RegistrationsController.ParticipantApprovalRequest
        {
            Amount = 0,
            Recipient = "team@example.com",
            ReturnUrl = "https://example.com/return"
        };

        var result = await controller.ApproveParticipantAsync(Guid.NewGuid(), request, CancellationToken.None);

        var validation = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, validation.StatusCode);
        workflow.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CompleteParticipantPaymentAsync_ReturnsNotFound_WhenWorkflowReturnsNull()
    {
        var repository = new Mock<IRegistrationRepository>();
        var workflow = new Mock<IRegistrationWorkflowService>();
        workflow
            .Setup(w => w.CompleteParticipantPaymentAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantRegistration?)null);

        var controller = CreateController(repository, workflow: workflow);

        var result = await controller.CompleteParticipantPaymentAsync(
            Guid.NewGuid(),
            new RegistrationsController.ParticipantPaymentCompletionRequest(),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task AnalyzeBusinessPlanAsync_ReturnsValidationProblem_WhenNarrativeEmpty()
    {
        var repository = new Mock<IRegistrationRepository>();
        var controller = CreateController(repository);

        var request = new RegistrationsController.BusinessPlanAnalysisRequest
        {
            Narrative = string.Empty
        };

        var result = await controller.AnalyzeBusinessPlanAsync(Guid.NewGuid(), request, CancellationToken.None);

        var validation = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, validation.StatusCode);
    }

    [Fact]
    public async Task AnalyzeBusinessPlanAsync_ReturnsNotFound_WhenWorkflowReturnsNull()
    {
        var repository = new Mock<IRegistrationRepository>();
        var workflow = new Mock<IRegistrationWorkflowService>();
        workflow
            .Setup(w => w.AnalyzeBusinessPlanAsync(It.IsAny<Guid>(), It.IsAny<BusinessPlanAnalysisOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusinessPlanReview?)null);

        var controller = CreateController(repository, workflow: workflow);

        var request = new RegistrationsController.BusinessPlanAnalysisRequest
        {
            Narrative = "طرح اولیه برای تحلیل"
        };

        var result = await controller.AnalyzeBusinessPlanAsync(Guid.NewGuid(), request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task AnalyzeBusinessPlanAsync_ReturnsReview_WhenWorkflowSucceeds()
    {
        var repository = new Mock<IRegistrationRepository>();
        var workflow = new Mock<IRegistrationWorkflowService>();
        var registrationId = Guid.NewGuid();
        var review = new BusinessPlanReview
        {
            Id = Guid.NewGuid(),
            ParticipantRegistrationId = registrationId,
            Status = BusinessPlanReviewStatus.Completed,
            Summary = "طرح قابل قبول است.",
            Strengths = "تیم چند تخصصی",
            Risks = "هزینه بازاریابی",
            Recommendations = "افزایش تحقیقات بازار",
            Model = "gemini-1.5-pro",
            CreatedAt = DateTimeOffset.UtcNow
        };

        workflow
            .Setup(w => w.AnalyzeBusinessPlanAsync(registrationId, It.IsAny<BusinessPlanAnalysisOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);

        var controller = CreateController(repository, workflow: workflow);

        var request = new RegistrationsController.BusinessPlanAnalysisRequest
        {
            Narrative = "شرح مدل کسب‌وکار"
        };

        var result = await controller.AnalyzeBusinessPlanAsync(registrationId, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<RegistrationsController.BusinessPlanReviewResponse>(ok.Value);
        Assert.Equal(review.Id, payload.Id);
        workflow.Verify(w => w.AnalyzeBusinessPlanAsync(registrationId, It.IsAny<BusinessPlanAnalysisOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListBusinessPlanReviewsAsync_ReturnsNotFound_WhenParticipantMissing()
    {
        var repository = new Mock<IRegistrationRepository>();
        repository
            .Setup(r => r.GetParticipantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantRegistration?)null);

        var controller = CreateController(repository);

        var result = await controller.ListBusinessPlanReviewsAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task ListBusinessPlanReviewsAsync_ReturnsReviews_WhenAvailable()
    {
        var repository = new Mock<IRegistrationRepository>();
        var registrationId = Guid.NewGuid();
        repository
            .Setup(r => r.GetParticipantAsync(registrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParticipantRegistration { Id = registrationId, TeamName = "Demo" });

        var reviews = new List<BusinessPlanReview>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ParticipantRegistrationId = registrationId,
                Status = BusinessPlanReviewStatus.Completed,
                Summary = "خلاصه",
                Strengths = "قدرت",
                Risks = "ریسک",
                Recommendations = "پیشنهاد",
                Model = "gemini",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        repository
            .Setup(r => r.ListBusinessPlanReviewsAsync(registrationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        var controller = CreateController(repository);

        var result = await controller.ListBusinessPlanReviewsAsync(registrationId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<List<RegistrationsController.BusinessPlanReviewResponse>>(ok.Value);
        Assert.Single(payload);
    }
}
