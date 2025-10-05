using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Operations.Models;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using NabTeams.Web.Controllers;
using Xunit;

namespace NabTeams.Api.Tests;

public class OperationsChecklistControllerTests
{
    private static OperationsChecklistController CreateController(
        Mock<IOperationsChecklistService> service,
        Mock<IAuditLogService>? auditLog = null,
        Mock<IOperationsArtifactStorage>? artifactStorage = null)
    {
        auditLog ??= new Mock<IAuditLogService>();
        auditLog.Setup(a => a.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                ActorId = "actor",
                ActorName = "actor",
                Action = "OperationsChecklist.Update",
                EntityType = nameof(OperationsChecklistItemEntity),
                EntityId = Guid.NewGuid().ToString(),
                CreatedAt = DateTimeOffset.UtcNow
            });

        artifactStorage ??= new Mock<IOperationsArtifactStorage>();

        return new OperationsChecklistController(service.Object, auditLog.Object, artifactStorage.Object)
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
    public async Task UpdateAsync_ReturnsUpdatedItemAndLogs()
    {
        var service = new Mock<IOperationsChecklistService>();
        var auditLog = new Mock<IAuditLogService>();
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

        auditLog.Setup(a => a.LogAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                ActorId = "actor",
                ActorName = "actor",
                Action = "OperationsChecklist.Update",
                EntityType = nameof(OperationsChecklistItemEntity),
                EntityId = itemId.ToString(),
                CreatedAt = DateTimeOffset.UtcNow
            });

        var controller = CreateController(service, auditLog);

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
        auditLog.Verify(a => a.LogAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            "OperationsChecklist.Update",
            nameof(OperationsChecklistItemEntity),
            itemId.ToString(),
            It.IsAny<object?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadArtifactAsync_ReturnsBadRequest_WhenFileMissing()
    {
        var service = new Mock<IOperationsChecklistService>();
        var controller = CreateController(service);

        var result = await controller.UploadArtifactAsync(Guid.NewGuid(), null, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("فایل", badRequest.Value?.ToString());
    }

    [Fact]
    public async Task UploadArtifactAsync_ReturnsNotFound_WhenItemMissing()
    {
        var service = new Mock<IOperationsChecklistService>();
        service.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("missing"));

        var controller = CreateController(service);
        var file = new FormFile(Stream.Null, 0, 0, "file", "report.txt");

        var result = await controller.UploadArtifactAsync(Guid.NewGuid(), file, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UploadArtifactAsync_SavesFileAndUpdatesItem()
    {
        var service = new Mock<IOperationsChecklistService>();
        var auditLog = new Mock<IAuditLogService>();
        var storage = new Mock<IOperationsArtifactStorage>();
        var id = Guid.NewGuid();
        var model = new OperationsChecklistItemModel(
            id,
            "security-scan",
            "اسکن امنیتی",
            "اجرای ZAP",
            "امنیت",
            OperationsChecklistStatus.InProgress,
            null,
            null,
            null,
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddHours(-2));

        var updatedModel = model with { ArtifactUrl = "/uploads/operations/security-scan/report.txt" };

        service.Setup(s => s.GetAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(model);
        service.Setup(s => s.UpdateAsync(id, It.IsAny<OperationsChecklistUpdateModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedModel);
        storage.Setup(s => s.SaveAsync(
                model.Key,
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredOperationsArtifact("report.txt", "/uploads/operations/security-scan/report.txt"));

        var controller = CreateController(service, auditLog, storage);
        var buffer = new MemoryStream(new byte[] { 1, 2, 3 });
        var file = new FormFile(buffer, 0, buffer.Length, "file", "report.txt");

        var result = await controller.UploadArtifactAsync(id, file, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<OperationsChecklistController.OperationsChecklistItemResponse>(ok.Value);
        Assert.Equal(updatedModel.ArtifactUrl, payload.ArtifactUrl);

        storage.Verify(s => s.SaveAsync(model.Key, "report.txt", It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        service.Verify(s => s.UpdateAsync(
            id,
            It.Is<OperationsChecklistUpdateModel>(m => m.ArtifactUrl == updatedModel.ArtifactUrl && m.Status == model.Status),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
