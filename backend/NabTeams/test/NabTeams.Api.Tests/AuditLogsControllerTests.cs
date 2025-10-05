using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NabTeams.Application.Abstractions;
using NabTeams.Domain.Entities;
using NabTeams.Web.Controllers;
using Xunit;

namespace NabTeams.Api.Tests;

public class AuditLogsControllerTests
{
    [Fact]
    public async Task ListAsync_ReturnsLogs()
    {
        var service = new Mock<IAuditLogService>();
        var logs = new List<AuditLogEntry>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ActorId = "admin",
                ActorName = "Admin",
                Action = "IntegrationSettings.Update",
                EntityType = "IntegrationSetting",
                EntityId = Guid.NewGuid().ToString(),
                Metadata = "{\"key\":\"value\"}",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
            }
        };

        service.Setup(s => s.ListAsync(null, null, 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        var controller = new AuditLogsController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            }
        };

        var result = await controller.ListAsync(null, null, 0, 100, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IEnumerable<AuditLogsController.AuditLogResponse>>(ok.Value);
        Assert.Single(payload);
    }
}
