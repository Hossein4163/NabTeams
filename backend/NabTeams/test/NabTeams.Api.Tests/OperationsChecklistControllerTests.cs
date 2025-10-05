using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Operations.Models;
using NabTeams.Domain.Enums;
using NabTeams.Web.Controllers;
using Xunit;

namespace NabTeams.Api.Tests;

public class OperationsChecklistControllerTests
{
    private static OperationsChecklistController CreateController(Mock<IOperationsChecklistService> service)
    {
        return new OperationsChecklistController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task ListAsync_ReturnsSeededItems()
    {
        var items = new List<OperationsChecklistItemModel>
        {
            new(Guid.NewGuid(), "security-scan", "Security", "Run ZAP", "امنیت", OperationsChecklistStatus.Pending, null, null, null, DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(-1)),
            new(Guid.NewGuid(), "load-test", "Load", "Execute k6", "کارایی", OperationsChecklistStatus.InProgress, null, "در حال اجرا", null, DateTimeOffset.UtcNow.AddDays(-3), DateTimeOffset.UtcNow.AddHours(-10))
        };
        var service = new Mock<IOperationsChecklistService>();
        service.Setup(s => s.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(items);
        var controller = CreateController(service);

        var result = await controller.ListAsync(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IEnumerable<OperationsChecklistController.OperationsChecklistItemResponse>>(ok.Value);
        Assert.Equal(2, payload.Count());
        Assert.Contains(payload, p => p.Key == "security-scan");
    }

    [Fact]
    public async Task UpdateAsync_ReturnsBadRequest_WhenStatusInvalid()
    {
        var service = new Mock<IOperationsChecklistService>();
        var controller = CreateController(service);
        var request = new OperationsChecklistController.OperationsChecklistUpdateRequest
        {
            Status = (OperationsChecklistStatus)99,
            Notes = "test"
        };

        var result = await controller.UpdateAsync(Guid.NewGuid(), request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("وضعیت", badRequest.Value?.ToString());
        service.Verify(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<OperationsChecklistUpdateModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFound_WhenItemMissing()
    {
        var service = new Mock<IOperationsChecklistService>();
        service.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<OperationsChecklistUpdateModel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("not found"));
        var controller = CreateController(service);

        var request = new OperationsChecklistController.OperationsChecklistUpdateRequest
        {
            Status = OperationsChecklistStatus.InProgress,
            Notes = "در حال اجرا"
        };

        var result = await controller.UpdateAsync(Guid.NewGuid(), request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedItem()
    {
        var service = new Mock<IOperationsChecklistService>();
        var itemId = Guid.NewGuid();
        var model = new OperationsChecklistItemModel(
            itemId,
            "security-scan",
            "اسکن امنیتی",
            "اجرای ZAP",
            "امنیت",
            OperationsChecklistStatus.Completed,
            DateTimeOffset.UtcNow.AddHours(-1),
            "گزارش در S3 ذخیره شد",
            "https://example.com/report",
            DateTimeOffset.UtcNow.AddDays(-2),
            DateTimeOffset.UtcNow.AddHours(-1));

        service.Setup(s => s.UpdateAsync(itemId, It.IsAny<OperationsChecklistUpdateModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);
        var controller = CreateController(service);

        var request = new OperationsChecklistController.OperationsChecklistUpdateRequest
        {
            Status = OperationsChecklistStatus.Completed,
            Notes = "گزارش در S3 ذخیره شد",
            ArtifactUrl = "https://example.com/report"
        };

        var result = await controller.UpdateAsync(itemId, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<OperationsChecklistController.OperationsChecklistItemResponse>(ok.Value);
        Assert.Equal("security-scan", payload.Key);
        Assert.Equal(OperationsChecklistStatus.Completed, payload.Status);
    }
}
