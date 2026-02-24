using System.Windows;
using DevGamingAutoInstaller.Models;
using DevGamingAutoInstaller.ViewModels;

namespace DevGamingAutoInstaller.Views;

public partial class SummaryWindow : Window
{
    public SummaryWindow(IEnumerable<InstallResult> results)
    {
        InitializeComponent();
        DataContext = new SummaryViewModel(results);
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
