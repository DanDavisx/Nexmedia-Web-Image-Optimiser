using System.Net.Http;
using System.Text.Json;
using System.Windows;
using NexMedia.WebImageOptimiser.Core.Releases;

namespace NexMedia.WebImageOptimiser.Desktop;

public partial class MainWindow
{
    private static readonly HttpClient ReleaseNotesClient = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly GitHubReleaseNotesService releaseNotesService = new(ReleaseNotesClient);
    private bool loadingReleaseNotes;

    private void ReleaseNotesNavigation_Click(object sender, RoutedEventArgs e)
    {
        bool show = ReleaseNotesNavigationButton.IsChecked == true;
        BackToOptimiser_Click(sender, e);
        ReleaseNotesNavigationButton.IsChecked = show;
        ReleaseNotesPage.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        MainHeaderCopy.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void RefreshReleaseNotes_Click(object sender, RoutedEventArgs e)
        => await LoadReleaseNotesAsync();

    private async Task LoadReleaseNotesAsync()
    {
        if (loadingReleaseNotes) return;
        loadingReleaseNotes = true;
        RefreshReleaseNotesButton.IsEnabled = false;
        ReleaseNotesStatus.Text = "Checking for release notes…";
        try
        {
            ReleaseNotes? notes = await releaseNotesService.GetLatestAsync();
            ReleaseNotesTitle.Text = notes?.Name is { Length: > 0 } name ? name : notes?.Tag ?? "";
            ReleaseNotesMetadata.Text = notes is null ? "" :
                $"{notes.Tag}  {notes.PublishedAt?.ToLocalTime().ToString("d MMM yyyy")}";
            ReleaseNotesBody.Text = notes?.Body ?? "";
            ReleaseNotesDot.Visibility = notes?.HasNotes == true ? Visibility.Visible : Visibility.Collapsed;
            ReleaseNotesStatus.Text = notes is null
                ? "No releases published yet."
                : notes.HasNotes ? "Latest published release" : "This release does not have release notes yet.";
            ReleaseNotesNavigationButton.ToolTip = notes?.HasNotes == true
                ? "Release notes available" : "Release notes";
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or JsonException)
        {
            ReleaseNotesStatus.Text = "Could not load release notes. Check your connection and try Refresh.";
            // Preserve any notes and indicator already loaded during this session.
        }
        finally
        {
            loadingReleaseNotes = false;
            RefreshReleaseNotesButton.IsEnabled = true;
        }
    }
}
