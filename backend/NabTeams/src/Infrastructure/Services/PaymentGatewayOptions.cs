namespace NabTeams.Infrastructure.Services;

public class PaymentGatewayOptions
{
    public string BaseUrl { get; set; } = "https://api.idpay.ir";
    public string ApiKey { get; set; } = string.Empty;
    public string CreatePath { get; set; } = "/v1.1/payment";
    public string VerifyPath { get; set; } = "/v1.1/payment/verify";
    public string CallbackBaseUrl { get; set; } = "https://example.com";
    public bool Sandbox { get; set; } = true;
}
