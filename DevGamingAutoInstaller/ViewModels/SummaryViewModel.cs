using System.Collections.ObjectModel;
using DevGamingAutoInstaller.Models;

namespace DevGamingAutoInstaller.ViewModels;

public sealed class SummaryViewModel : ObservableObject
{
    public SummaryViewModel(IEnumerable<InstallResult> results)
    {
        Results = new ObservableCollection<InstallResult>(results);
        SuccessCount = Results.Count(r => r.Status == InstallStatus.Success);
        FailedCount = Results.Count(r => r.Status == InstallStatus.Failed);
        SkippedCount = Results.Count(r => r.Status == InstallStatus.AlreadyInstalled || r.Status == InstallStatus.Skipped);
    }

    public ObservableCollection<InstallResult> Results { get; }
    public int SuccessCount { get; }
    public int FailedCount { get; }
    public int SkippedCount { get; }
}
