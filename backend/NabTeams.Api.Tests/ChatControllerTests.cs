using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using NabTeams.Api.Configuration;
using NabTeams.Api.Controllers;
using NabTeams.Api.Models;
using NabTeams.Api.Services;
using NabTeams.Api.Stores;
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
    public async Task Rejects_OverlyLongMessages()
    {
        var repository = new Mock<IChatRepository>(MockBehavior.Strict);
        var rateLimiter = new Mock<IRateLimiter>(MockBehavior.Strict);
        var queue = new Mock<IChatModerationQueue>(MockBehavior.Strict);
        var controller = CreateController(repository.Object, rateLimiter.Object, queue.Object);

        var longContent = new string('ا', SendMessageRequest.MaxContentLength + 1);
        var result = await controller.SendMessageAsync("participant", new SendMessageRequest { Content = longContent }, default);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("حداکثر", badRequest.Value?.ToString(), StringComparison.Ordinal);
        repository.VerifyNoOtherCalls();
        rateLimiter.VerifyNoOtherCalls();
        queue.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Accepts_MessageWithinLimit()
    {
        var repository = new Mock<IChatRepository>();
        repository.Setup(r => r.AddMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.GetMessagesAsync(RoleChannel.Participant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Message>());

        var rateLimiter = new Mock<IRateLimiter>();
        rateLimiter.Setup(r => r.CheckQuota("user-1", RoleChannel.Participant))
            .Returns(new RateLimitResult(true, null, null));

        var queue = new Mock<IChatModerationQueue>();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<ChatModerationWorkItem>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask(Task.CompletedTask));

        var controller = CreateController(repository.Object, rateLimiter.Object, queue.Object);

        var actionResult = await controller.SendMessageAsync("participant", new SendMessageRequest { Content = " سلام " }, default);
        var accepted = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(StatusCodes.Status202Accepted, accepted.StatusCode);
        var payload = Assert.IsType<SendMessageResponse>(accepted.Value);
        Assert.Equal(MessageStatus.Held, payload.Status);

        repository.Verify(r => r.AddMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Once);
        queue.Verify(q => q.EnqueueAsync(It.IsAny<ChatModerationWorkItem>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
