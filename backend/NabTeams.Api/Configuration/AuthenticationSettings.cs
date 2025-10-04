namespace NabTeams.Api.Configuration;

public class AuthenticationSettings
{
    public bool Disabled { get; set; }
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = true;
    public string AdminRole { get; set; } = "admin";
    public string? NameClaimType { get; set; }
    public string? RoleClaimType { get; set; }
}

public static class AuthorizationPolicies
{
    public const string Admin = "AdminOnly";
}
