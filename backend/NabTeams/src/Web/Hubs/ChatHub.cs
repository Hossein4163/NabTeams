using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using NabTeams.Web.Configuration;

namespace NabTeams.Web.Hubs;

public interface IChatClient
{
    Task MessageUpserted(Message message);
}

[Authorize]
public class ChatHub : Hub<IChatClient>
{
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var requestedRole = httpContext?.Request.Query["role"].ToString();

        if (!RoleChannelExtensions.TryParse(requestedRole, out var channel) || !HasChannelAccess(channel))
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, BuildGroupName(channel));
        await base.OnConnectedAsync();
    }

    internal static string BuildGroupName(RoleChannel channel)
        => $"chat-{channel.ToString().ToLowerInvariant()}";

    private bool HasChannelAccess(RoleChannel channel)
    {
        if (IsAdmin())
        {
            return true;
        }

        var normalized = channel.ToString().ToLowerInvariant();
        var roles = Context.User?.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type.Equals("role", StringComparison.OrdinalIgnoreCase) || c.Type.Equals("roles", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value.ToLowerInvariant())
            .ToHashSet();

        return roles is not null && roles.Contains(normalized);
    }

    private bool IsAdmin()
    {
        var adminRole = Context.GetHttpContext()?.RequestServices.GetService<IOptions<AuthenticationSettings>>()?.Value.AdminRole ?? "admin";
        if (string.IsNullOrWhiteSpace(adminRole))
        {
            return false;
        }

        if (Context.User?.IsInRole(adminRole) == true)
        {
            return true;
        }

        var normalizedAdmin = adminRole.ToLowerInvariant();
        return Context.User?.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type.Equals("role", StringComparison.OrdinalIgnoreCase) || c.Type.Equals("roles", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Value.ToLowerInvariant())
            .Contains(normalizedAdmin) == true;
    }
}
