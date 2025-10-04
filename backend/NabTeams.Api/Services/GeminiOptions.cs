namespace NabTeams.Api.Services;

public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public string ModerationModel { get; set; } = "gemini-1.5-pro";
    public string RagModel { get; set; } = "gemini-1.5-pro";
    public bool Enabled => !string.IsNullOrWhiteSpace(ApiKey);
}
