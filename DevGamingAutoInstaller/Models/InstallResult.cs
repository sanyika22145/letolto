namespace DevGamingAutoInstaller.Models;

public sealed class InstallResult
{
    public string Name { get; init; } = string.Empty;
    public InstallStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
}
