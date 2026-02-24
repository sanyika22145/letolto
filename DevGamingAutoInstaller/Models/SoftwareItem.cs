using DevGamingAutoInstaller.ViewModels;

namespace DevGamingAutoInstaller.Models;

public sealed class SoftwareItem : ObservableObject
{
    private bool _isSelected;
    private bool _isInstalled;
    private InstallStatus _status;
    private string _statusMessage = string.Empty;

    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? DownloadUrl { get; init; }
    public string SilentArgs { get; init; } = string.Empty;
    public bool RequiresAdmin { get; init; }
    public InstallMethod InstallMethod { get; init; } = InstallMethod.DownloadAndRun;
    public string? Command { get; init; }
    public string? CommandArgs { get; init; }
    public string? DetectDisplayNameContains { get; init; }
    public string? DetectFilePath { get; init; }
    public List<string> PostInstallCommands { get; init; } = new();

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsInstalled
    {
        get => _isInstalled;
        set => SetProperty(ref _isInstalled, value);
    }

    public InstallStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
}
