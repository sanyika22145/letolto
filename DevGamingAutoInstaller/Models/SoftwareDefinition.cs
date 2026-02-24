namespace DevGamingAutoInstaller.Models;

public sealed class SoftwareDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? DownloadUrl { get; set; }
    public string SilentArgs { get; set; } = string.Empty;
    public bool RequiresAdmin { get; set; }
    public InstallMethod InstallMethod { get; set; } = InstallMethod.DownloadAndRun;
    public string? Command { get; set; }
    public string? CommandArgs { get; set; }
    public string? DetectDisplayNameContains { get; set; }
    public string? DetectFilePath { get; set; }
    public List<string> PostInstallCommands { get; set; } = new();
}
