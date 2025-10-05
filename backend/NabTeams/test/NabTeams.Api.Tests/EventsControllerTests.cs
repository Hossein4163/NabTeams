using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using NabTeams.Web.Controllers;
using Xunit;

namespace NabTeams.Api.Tests;

public class EventsControllerTests
{
    [Fact]
    public async Task ListAsync_ReturnsEvents()
    {
        var events = new List<EventDetail>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Demo Event",
                Description = "Test",
                AiTaskManagerEnabled = true,
                StartsAt = DateTimeOffset.UtcNow,
                EndsAt = DateTimeOffset.UtcNow.AddDays(1),
                SampleTasks = new[]
                {
                    new ParticipantTask
                    {
                        Title = "Kickoff",
                        Status = ParticipantTaskStatus.Todo
                    }
                }
            }
        };

        var service = new Mock<IEventService>();
        service.Setup(s => s.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var controller = new EventsController(service.Object);

        var result = await controller.ListAsync(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IReadOnlyCollection<EventsController.EventResponse>>(ok.Value);
        Assert.Single(payload);
        Assert.True(payload.First().AiTaskManagerEnabled);
    }

    [Fact]
    public async Task GetAsync_ReturnsNotFound_WhenMissing()
    {
        var service = new Mock<IEventService>();
        service.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventDetail?)null);

        var controller = new EventsController(service.Object);

        var result = await controller.GetAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetAsync_ReturnsEvent_WhenFound()
    {
        var detail = new EventDetail
        {
            Id = Guid.NewGuid(),
            Name = "AI Summit",
            AiTaskManagerEnabled = false
        };

        var service = new Mock<IEventService>();
        service.Setup(s => s.GetAsync(detail.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        var controller = new EventsController(service.Object);

        var result = await controller.GetAsync(detail.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<EventsController.EventResponse>(ok.Value);
        Assert.Equal(detail.Name, payload.Name);
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreated()
    {
        var detail = new EventDetail
        {
            Id = Guid.NewGuid(),
            Name = "Hackathon",
            AiTaskManagerEnabled = true
        };

        var service = new Mock<IEventService>();
        service.Setup(s => s.CreateAsync(It.IsAny<EventDetail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);

        var controller = new EventsController(service.Object);

        var request = new EventsController.EventRequest
        {
            Name = detail.Name,
            AiTaskManagerEnabled = true
        };

        var result = await controller.CreateAsync(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(EventsController.GetAsync), created.ActionName);
        var payload = Assert.IsType<EventsController.EventResponse>(created.Value);
        Assert.Equal(detail.Name, payload.Name);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenMissing()
    {
        var service = new Mock<IEventService>();
        service.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<EventDetail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventDetail?)null);

        var controller = new EventsController(service.Object);

        var request = new EventsController.EventRequest
        {
            Name = "Missing",
            AiTaskManagerEnabled = false
        };

        var result = await controller.UpdateAsync(Guid.NewGuid(), request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNoContent()
    {
        var service = new Mock<IEventService>();
        service.Setup(s => s.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = new EventsController(service.Object);

        var result = await controller.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }
}
