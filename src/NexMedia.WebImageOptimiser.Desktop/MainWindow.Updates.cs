using System.Windows;
using NexMedia.WebImageOptimiser.Desktop.Configuration;

namespace NexMedia.WebImageOptimiser.Desktop;

public partial class MainWindow
{
    private readonly AppUpdateService appUpdates = new();

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        await CheckForUpdatesAsync(manual: true);
    }

    private async Task CheckForUpdatesAsync(bool manual)
    {
        CheckForUpdatesButton.IsEnabled = false;
        UpdateStatusText.Text = "Checking for updates; new versions download automatically…";
        try
        {
            UpdateStatusText.Text = await appUpdates.CheckAndDownloadAsync(manual);
        }
        finally
        {
            CheckForUpdatesButton.IsEnabled = true;
        }
    }
}
