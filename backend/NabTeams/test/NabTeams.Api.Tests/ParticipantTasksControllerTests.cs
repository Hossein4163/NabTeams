using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Tasks.Models;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using NabTeams.Web.Controllers;
using Xunit;

namespace NabTeams.Api.Tests;

public class ParticipantTasksControllerTests
{
    [Fact]
    public async Task ListAsync_ReturnsTasks()
    {
        var tasks = new List<ParticipantTask>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ParticipantRegistrationId = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                Title = "Design landing page",
                Status = ParticipantTaskStatus.InProgress
            }
        };

        var service = new Mock<IParticipantTaskService>();
        service.Setup(s => s.ListAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        var controller = new ParticipantTasksController(service.Object);

        var result = await controller.ListAsync(Guid.NewGuid(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IReadOnlyCollection<RegistrationsController.ParticipantTaskResponse>>(ok.Value);
        Assert.Single(payload);
    }

    [Fact]
    public async Task ListAsync_ReturnsBadRequest_WhenServiceThrows()
    {
        var service = new Mock<IParticipantTaskService>();
        service.Setup(s => s.ListAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("disabled"));

        var controller = new ParticipantTasksController(service.Object);

        var result = await controller.ListAsync(Guid.NewGuid(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var details = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("disabled", details.Detail);
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreated()
    {
        var participantId = Guid.NewGuid();
        var task = new ParticipantTask
        {
            Id = Guid.NewGuid(),
            ParticipantRegistrationId = participantId,
            EventId = Guid.NewGuid(),
            Title = "Prepare pitch"
        };

        var service = new Mock<IParticipantTaskService>();
        service.Setup(s => s.CreateAsync(participantId, It.IsAny<ParticipantTaskInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var controller = new ParticipantTasksController(service.Object);

        var request = new ParticipantTasksController.ParticipantTaskRequest
        {
            EventId = task.EventId,
            Title = task.Title
        };

        var result = await controller.CreateAsync(participantId, request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var payload = Assert.IsType<RegistrationsController.ParticipantTaskResponse>(created.Value);
        Assert.Equal(task.Title, payload.Title);
    }

    [Fact]
    public async Task CreateAsync_ReturnsBadRequest_WhenServiceThrows()
    {
        var service = new Mock<IParticipantTaskService>();
        service.Setup(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<ParticipantTaskInput>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("not allowed"));

        var controller = new ParticipantTasksController(service.Object);

        var request = new ParticipantTasksController.ParticipantTaskRequest
        {
            EventId = Guid.NewGuid(),
            Title = "Invalid"
        };

        var result = await controller.CreateAsync(Guid.NewGuid(), request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var details = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("not allowed", details.Detail);
    }

    [Fact]
    public async Task UpdateStatusAsync_ReturnsNotFound_WhenMissing()
    {
        var service = new Mock<IParticipantTaskService>();
        service.Setup(s => s.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<ParticipantTaskStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantTask?)null);

        var controller = new ParticipantTasksController(service.Object);

        var result = await controller.UpdateStatusAsync(Guid.NewGuid(), Guid.NewGuid(), new ParticipantTasksController.ParticipantTaskStatusRequest
        {
            Status = ParticipantTaskStatus.Completed
        }, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsBadRequest_WhenServiceThrows()
    {
        var service = new Mock<IParticipantTaskService>();
        service.Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("blocked"));

        var controller = new ParticipantTasksController(service.Object);

        var result = await controller.DeleteAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var details = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("blocked", details.Detail);
    }

    [Fact]
    public async Task GenerateAdviceAsync_ReturnsResult()
    {
        var participantId = Guid.NewGuid();
        var advice = new TaskAdviceResult
        {
            Summary = "Plan sprint",
            SuggestedTasks = new[] { "Deploy MVP" },
            Risks = "Scope creep",
            NextSteps = "Focus backlog"
        };

        var service = new Mock<IParticipantTaskService>();
        service.Setup(s => s.GenerateAdviceAsync(participantId, It.IsAny<TaskAdviceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(advice);

        var controller = new ParticipantTasksController(service.Object);

        var result = await controller.GenerateAdviceAsync(participantId, new ParticipantTasksController.ParticipantTaskAdviceRequest
        {
            Context = "Need roadmap",
            FocusArea = "Product"
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ParticipantTasksController.TaskAdviceResponse>(ok.Value);
        Assert.Equal(advice.Summary, payload.Summary);
    }
}
