namespace NabTeams.Infrastructure.Services;

public class NotificationOptions
{
    public EmailOptions Email { get; set; } = new();
    public SmsOptions Sms { get; set; } = new();

    public class EmailOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string SenderAddress { get; set; } = string.Empty;
        public string SenderDisplayName { get; set; } = "NabTeams";
    }

    public class SmsOptions
    {
        public string Provider { get; set; } = "kavenegar";
        public string BaseUrl { get; set; } = "https://api.kavenegar.com";
        public string ApiKey { get; set; } = string.Empty;
        public string SenderNumber { get; set; } = string.Empty;
        public string Path { get; set; } = "v1/{apiKey}/sms/send.json";
    }
}
