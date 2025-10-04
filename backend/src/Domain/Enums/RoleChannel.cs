namespace NabTeams.Domain.Enums;

public enum RoleChannel
{
    Participant,
    Judge,
    Mentor,
    Investor,
    Admin
}

public static class RoleChannelExtensions
{
    public static bool TryParse(string value, out RoleChannel channel)
    {
        if (Enum.TryParse<RoleChannel>(value, true, out var parsed))
        {
            channel = parsed;
            return true;
        }

        channel = default;
        return false;
    }

    public static string ToRouteSegment(this RoleChannel channel) => channel.ToString().ToLowerInvariant();
}
