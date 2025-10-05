namespace NabTeams.Infrastructure.Services;

public class FileStorageOptions
{
    public string RootPath { get; set; } = "storage/uploads";

    public string PublicBaseUrl { get; set; } = "/uploads";
}
