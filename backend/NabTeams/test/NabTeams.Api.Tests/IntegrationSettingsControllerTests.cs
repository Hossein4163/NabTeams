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

public class IntegrationSettingsControllerTests
{
    private static IntegrationSettingsController CreateController(Mock<IIntegrationSettingsService> service)
    {
        return new IntegrationSettingsController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task ListAsync_ReturnsItems_WhenTypeValid()
    {
        var service = new Mock<IIntegrationSettingsService>();
        service.Setup(s => s.ListAsync(IntegrationProviderType.Gemini, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IntegrationSetting>
            {
                new IntegrationSetting
                {
                    Id = Guid.NewGuid(),
                    Type = IntegrationProviderType.Gemini,
                    ProviderKey = "gemini",
                    DisplayName = "Gemini",
                    Configuration = "{}",
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                    UpdatedAt = DateTimeOffset.UtcNow
                }
            });

        var controller = CreateController(service);

        var result = await controller.ListAsync("Gemini", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsAssignableFrom<IEnumerable<IntegrationSettingsController.IntegrationSettingResponse>>(ok.Value);
        Assert.Single(payload);
        Assert.Equal("gemini", payload.First().ProviderKey);
    }

    [Fact]
    public async Task ListAsync_ReturnsBadRequest_WhenTypeInvalid()
    {
        var service = new Mock<IIntegrationSettingsService>();
        var controller = CreateController(service);

        var result = await controller.ListAsync("invalid", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("نوع", badRequest.Value?.ToString());
        service.Verify(s => s.ListAsync(It.IsAny<IntegrationProviderType?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpsertAsync_ReturnsValidationProblem_WhenProviderKeyMissing()
    {
        var service = new Mock<IIntegrationSettingsService>();
        var controller = CreateController(service);

        var request = new IntegrationSettingsController.IntegrationSettingUpsertRequest
        {
            Type = "Gemini",
            ProviderKey = ""
        };

        var result = await controller.UpsertAsync(request, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(400, problem.StatusCode);
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpsertAsync_DelegatesToService_WhenPayloadValid()
    {
        var service = new Mock<IIntegrationSettingsService>();
        var saved = new IntegrationSetting
        {
            Id = Guid.NewGuid(),
            Type = IntegrationProviderType.Gemini,
            ProviderKey = "gemini",
            DisplayName = "Gemini",
            Configuration = "{}",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        service.Setup(s => s.UpsertAsync(It.IsAny<IntegrationSetting>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(saved);

        var controller = CreateController(service);

        var request = new IntegrationSettingsController.IntegrationSettingUpsertRequest
        {
            Type = "Gemini",
            ProviderKey = "gemini",
            DisplayName = "Gemini",
            Configuration = "{}",
            Activate = true
        };

        var result = await controller.UpsertAsync(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<IntegrationSettingsController.IntegrationSettingResponse>(ok.Value);
        Assert.Equal(saved.Id, payload.Id);
        service.Verify(s => s.UpsertAsync(It.IsAny<IntegrationSetting>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateAsync_CallsService()
    {
        var service = new Mock<IIntegrationSettingsService>();
        var controller = CreateController(service);
        var id = Guid.NewGuid();

        var result = await controller.ActivateAsync(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        service.Verify(s => s.SetActiveAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
