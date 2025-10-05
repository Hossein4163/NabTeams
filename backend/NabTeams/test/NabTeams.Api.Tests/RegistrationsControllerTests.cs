using System;
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
        Mock<IRegistrationDocumentStorage>? storage = null)
    {
        storage ??= new Mock<IRegistrationDocumentStorage>();
        return new RegistrationsController(repository.Object, storage.Object);
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
    public async Task FinalizeParticipantAsync_ReturnsOk_WhenRepositoryUpdatesStatus()
    {
        var repository = new Mock<IRegistrationRepository>();
        var registrationId = Guid.NewGuid();
        var finalized = new ParticipantRegistration
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
            Status = RegistrationStatus.Finalized,
            FinalizedAt = DateTimeOffset.UtcNow,
            SubmittedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };

        repository
            .Setup(r => r.FinalizeParticipantAsync(registrationId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(finalized);

        var controller = CreateController(repository);

        var result = await controller.FinalizeParticipantAsync(registrationId, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<RegistrationsController.ParticipantRegistrationResponse>(ok.Value);
        Assert.Equal(RegistrationStatus.Finalized, payload.Status);
        repository.Verify(r => r.FinalizeParticipantAsync(registrationId, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
