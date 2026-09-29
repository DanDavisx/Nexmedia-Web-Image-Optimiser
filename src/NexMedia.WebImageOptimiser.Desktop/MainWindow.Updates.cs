using System.Windows;
using NexMedia.WebImageOptimiser.Desktop.Configuration;

namespace NexMedia.WebImageOptimiser.Desktop;

public partial class MainWindow
{
    private readonly AppUpdateService appUpdates = new();
    private bool checkingForUpdates;
    private AppUpdateStatus updateStatus = new(AppUpdateState.Idle, "");

    private void SetUpdateStatus(AppUpdateStatus status)
    {
        updateStatus = status;
        UpdateStatusText.Text = status.Message;
        foreach (var card in releaseCards) card.Update(status);
    }

    private async void ReleaseUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (isOptimising || isExporting || ViewModel.IsImporting)
        {
            MessageBox.Show(this, "Wait for the current operation to finish before updating.", "Update");
            return;
        }
        if (sender is FrameworkElement { DataContext: ViewModels.ReleaseNoteCardViewModel { IsReady: true } })
        {
            try { appUpdates.RestartToApply(); }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.TraceWarning("Update restart failed: {0}", exception);
                SetUpdateStatus(new(AppUpdateState.Failed, "Could not restart to update. Refresh and try again.", updateStatus.Version));
            }
        }
        else await CheckForUpdatesAsync(manual: true);
    }

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        await CheckForUpdatesAsync(manual: true);
    }

    private async Task CheckForUpdatesAsync(bool manual)
    {
        if (checkingForUpdates) return;
        checkingForUpdates = true;
        CheckForUpdatesButton.IsEnabled = false;
        SetUpdateStatus(new(AppUpdateState.Checking, "Checking for updates; new versions download automatically…"));
        try
        {
            var progress = new Progress<AppUpdateStatus>(status =>
            {
                if (checkingForUpdates) SetUpdateStatus(status);
            });
            SetUpdateStatus(await appUpdates.CheckAndDownloadAsync(manual, progress));
        }
        finally
        {
            checkingForUpdates = false;
            CheckForUpdatesButton.IsEnabled = true;
        }
    }
}
