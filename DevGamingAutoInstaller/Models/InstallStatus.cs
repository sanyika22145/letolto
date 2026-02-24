namespace DevGamingAutoInstaller.Models;

public enum InstallStatus
{
    Pending,
    Downloading,
    Installing,
    PostInstall,
    Success,
    Failed,
    Skipped,
    AlreadyInstalled
}
