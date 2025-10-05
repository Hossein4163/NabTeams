namespace NabTeams.Application.Common;

public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
    public string ModerationModel { get; set; } = "gemini-1.5-pro";
    public string RagModel { get; set; } = "gemini-1.5-pro";
    public string BusinessPlanModel { get; set; } = "gemini-1.5-pro";
    public double BusinessPlanTemperature { get; set; } = 0.2;
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
    public bool Enabled => !string.IsNullOrWhiteSpace(ApiKey);
}
