using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DevGamingAutoInstaller.Models;
using DevGamingAutoInstaller.Services;

namespace DevGamingAutoInstaller.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly InstallerService _installerService;
    private readonly SoftwareCatalog _softwareCatalog;
    private readonly Logger _logger;
    private bool _isInstalling;
    private double _overallProgress;
    private string _progressMessage = string.Empty;
    private string _statusMessage = string.Empty;

    public MainViewModel()
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, "log.txt");
        _logger = new Logger(logPath);
        _softwareCatalog = new SoftwareCatalog(_logger);
        _installerService = new InstallerService(new HttpClient(), _logger);

        SoftwareItems = new ObservableCollection<SoftwareItem>();

        StartInstallCommand = new AsyncRelayCommand(StartInstallAsync, CanStartInstall);
        SelectAllCommand = new RelayCommand(SelectAll, () => !IsInstalling);
        SelectDevCommand = new RelayCommand(() => SelectCategory("Fejlesztői eszközök"), () => !IsInstalling);
        SelectGameCommand = new RelayCommand(() => SelectCategory("Játék platformok"), () => !IsInstalling);
        SelectGeneralCommand = new RelayCommand(() => SelectCategory("Általános programok"), () => !IsInstalling);
        RefreshInstalledCommand = new AsyncRelayCommand(RefreshInstalledAsync, () => !IsInstalling);
    }

    public ObservableCollection<SoftwareItem> SoftwareItems { get; }

    public Dictionary<string, SoftwareDefinition> DefinitionDictionary { get; private set; } = new();

    public bool IsInstalling
    {
        get => _isInstalling;
        private set
        {
            if (SetProperty(ref _isInstalling, value))
            {
                RaiseAllCanExecute();
            }
        }
    }

    public double OverallProgress
    {
        get => _overallProgress;
        private set => SetProperty(ref _overallProgress, value);
    }

    public string ProgressMessage
    {
        get => _progressMessage;
        private set => SetProperty(ref _progressMessage, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public AsyncRelayCommand StartInstallCommand { get; }
    public RelayCommand SelectAllCommand { get; }
    public RelayCommand SelectDevCommand { get; }
    public RelayCommand SelectGameCommand { get; }
    public RelayCommand SelectGeneralCommand { get; }
    public AsyncRelayCommand RefreshInstalledCommand { get; }

    public async Task InitializeAsync()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "software.json");
        var items = await _softwareCatalog.LoadAsync(configPath);
        DefinitionDictionary = _softwareCatalog.GetDefinitionDictionary();
        OnPropertyChanged(nameof(DefinitionDictionary));

        Application.Current.Dispatcher.Invoke(() =>
        {
            SoftwareItems.Clear();
            foreach (var item in items)
            {
                item.Status = InstallStatus.Pending;
                item.StatusMessage = "Készen áll";
                SoftwareItems.Add(item);
            }
        });

        await RefreshInstalledAsync();
    }

    private async Task RefreshInstalledAsync()
    {
        await Task.Run(() =>
        {
            foreach (var item in SoftwareItems)
            {
                var installed = _installerService.IsInstalled(item);
                Application.Current.Dispatcher.Invoke(() => item.IsInstalled = installed);
            }
        });

        Application.Current.Dispatcher.Invoke(() =>
        {
            StatusMessage = "Telepített programok ellenőrizve.";
        });
    }

    private async Task StartInstallAsync()
    {
        var selected = SoftwareItems.Where(item => item.IsSelected).ToList();
        if (selected.Count == 0)
        {
            StatusMessage = "Nincs kiválasztott program.";
            return;
        }

        IsInstalling = true;
        StatusMessage = "Telepítés folyamatban...";
        OverallProgress = 0;
        ProgressMessage = string.Empty;

        var progress = new Progress<InstallProgress>(update =>
        {
            OverallProgress = update.OverallPercent;
            ProgressMessage = $"{update.CurrentItem}: {update.Message}";
        });

        foreach (var item in selected)
        {
            item.Status = InstallStatus.Pending;
            item.StatusMessage = "Sorban áll";
        }

        var results = await _installerService.InstallAsync(
            selected,
            progress,
            (item, status, message) => Application.Current.Dispatcher.Invoke(() =>
            {
                item.Status = status;
                item.StatusMessage = message;
                if (status == InstallStatus.Success)
                {
                    item.IsInstalled = true;
                }
            }),
            CancellationToken.None);

        IsInstalling = false;
        StatusMessage = "Telepítés befejezve.";

        Application.Current.Dispatcher.Invoke(() =>
        {
            var summaryWindow = new Views.SummaryWindow(results);
            summaryWindow.Owner = Application.Current.MainWindow;
            summaryWindow.ShowDialog();
        });
    }

    private bool CanStartInstall() => !IsInstalling;

    private void SelectAll()
    {
        foreach (var item in SoftwareItems)
        {
            item.IsSelected = true;
        }
    }

    private void SelectCategory(string category)
    {
        foreach (var item in SoftwareItems)
        {
            item.IsSelected = string.Equals(item.Category, category, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void RaiseAllCanExecute()
    {
        StartInstallCommand.RaiseCanExecuteChanged();
        SelectAllCommand.RaiseCanExecuteChanged();
        SelectDevCommand.RaiseCanExecuteChanged();
        SelectGameCommand.RaiseCanExecuteChanged();
        SelectGeneralCommand.RaiseCanExecuteChanged();
        RefreshInstalledCommand.RaiseCanExecuteChanged();
    }
}
