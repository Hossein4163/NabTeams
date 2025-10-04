using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Common;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using NabTeams.Web.Configuration;
using NabTeams.Web.Controllers;
using Xunit;

namespace NabTeams.Api.Tests;

public class ChatControllerTests
{
    private static ChatController CreateController(
        IChatRepository repository,
        IRateLimiter rateLimiter,
        IChatModerationQueue queue)
    {
        var controller = new ChatController(
            repository,
            rateLimiter,
            queue,
            Options.Create(new AuthenticationSettings { AdminRole = "admin" }));

        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim("sub", "user-1"),
                new Claim(ClaimTypes.Role, "participant")
            },
            "TestAuth");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        return controller;
    }

    [Fact]
    public async Task SendMessageAsync_ReturnsAccepted_WhenValid()
    {
        var repo = new Mock<IChatRepository>();
        repo.Setup(r => r.AddMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var rateLimiter = new Mock<IRateLimiter>();
        rateLimiter.Setup(r => r.CheckQuota(It.IsAny<string>(), It.IsAny<RoleChannel>()))
            .Returns(new RateLimitResult(true, null, null));
        var queue = new Mock<IChatModerationQueue>();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<ChatModerationWorkItem>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());

        var controller = CreateController(repo.Object, rateLimiter.Object, queue.Object);
        var request = new SendMessageRequest { Content = "hello" };

        var result = await controller.SendMessageAsync("participant", request, CancellationToken.None);
        var accepted = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status202Accepted, accepted.StatusCode);
    }

    [Fact]
    public async Task SendMessageAsync_ReturnsTooLong_WhenExceedsLimit()
    {
        var controller = CreateController(
            Mock.Of<IChatRepository>(),
            Mock.Of<IRateLimiter>(l => l.CheckQuota(It.IsAny<string>(), It.IsAny<RoleChannel>()) == new RateLimitResult(true, null, null)),
            Mock.Of<IChatModerationQueue>());

        var request = new SendMessageRequest { Content = new string('a', SendMessageRequest.MaxContentLength + 1) };
        var result = await controller.SendMessageAsync("participant", request, CancellationToken.None);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("حداکثر طول", badRequest.Value?.ToString());
    }
}
