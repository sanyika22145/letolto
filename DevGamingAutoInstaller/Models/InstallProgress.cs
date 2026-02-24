namespace DevGamingAutoInstaller.Models;

public sealed class InstallProgress
{
    public double OverallPercent { get; init; }
    public string CurrentItem { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
