using System.Net.Http;
using System.Net;
using System.Text.Json;
using System.Windows;
using NexMedia.WebImageOptimiser.Core.Releases;

namespace NexMedia.WebImageOptimiser.Desktop;

public partial class MainWindow
{
    private static readonly HttpClient ReleaseNotesClient = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly GitHubReleaseNotesService releaseNotesService = new(ReleaseNotesClient);
    private bool loadingReleaseNotes;
    private bool refreshingReleases;
    private DateTimeOffset nextReleaseRefresh;
    private readonly System.Windows.Threading.DispatcherTimer releaseRefreshTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private ViewModels.ReleaseNoteCardViewModel[] releaseCards = [];
    private readonly ReleaseNotesReadState releaseNotesReadState = new(System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NexMedia", "WebImageOptimiser", "viewed-release.txt"));
    private ReleaseNotes? latestRelease;

    private void ReleaseNotesNavigation_Click(object sender, RoutedEventArgs e)
    {
        bool show = ReleaseNotesNavigationButton.IsChecked == true;
        BackToOptimiser_Click(sender, e);
        ReleaseNotesNavigationButton.IsChecked = show;
        SetOptimiserVisibility(!show);
        ReleaseNotesPage.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        MainHeaderCopy.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
        UpdateReleaseNotesIndicator();
    }

    private async void RefreshReleaseNotes_Click(object sender, RoutedEventArgs e)
        => await RefreshReleasesAsync();

    private void UpdateReleaseRefreshControls()
    {
        bool coolingDown = DateTimeOffset.UtcNow < nextReleaseRefresh;
        bool enabled = !refreshingReleases && !loadingReleaseNotes && !checkingForUpdates && !coolingDown;
        RefreshReleaseNotesButton.IsEnabled = enabled;
        CheckForUpdatesButton.IsEnabled = enabled;
        RefreshReleaseNotesButton.ToolTip = coolingDown
            ? $"Refresh available in {Math.Ceiling((nextReleaseRefresh - DateTimeOffset.UtcNow).TotalSeconds)} seconds."
            : "Refresh release notes and check for updates";
        foreach (var card in releaseCards) card.SetUpdateActionEnabled(card.IsReady || enabled);
        if (!coolingDown) releaseRefreshTimer.Stop();
    }

    private async Task RefreshReleasesAsync()
    {
        if (refreshingReleases || loadingReleaseNotes || checkingForUpdates || DateTimeOffset.UtcNow < nextReleaseRefresh) return;
        refreshingReleases = true;
        nextReleaseRefresh = DateTimeOffset.UtcNow.AddSeconds(30);
        releaseRefreshTimer.Start();
        UpdateReleaseRefreshControls();
        try { await Task.WhenAll(LoadReleaseNotesAsync(forceRefresh: true), CheckForUpdatesAsync(manual: true)); }
        finally
        {
            refreshingReleases = false;
            UpdateReleaseRefreshControls();
        }
    }

    private void UpdateReleaseNotesIndicator()
    {
        if (ReleaseNotesPage.Visibility == Visibility.Visible && latestRelease is not null)
            releaseNotesReadState.MarkViewed(latestRelease);

        bool unread = releaseNotesReadState.IsUnread(latestRelease);
        ReleaseNotesDot.Visibility = unread ? Visibility.Visible : Visibility.Collapsed;
        ReleaseNotesNavigationButton.ToolTip = unread ? "Unread release notes available" : "Release notes";
    }

    private async Task LoadReleaseNotesAsync(bool forceRefresh = false)
    {
        if (loadingReleaseNotes) return;
        loadingReleaseNotes = true;
        UpdateReleaseRefreshControls();
        ReleaseNotesStatus.Text = "Checking for release notes…";
        ReleaseNotesStatus.Visibility = Visibility.Visible;
        try
        {
            var history = await releaseNotesService.GetHistoryAsync(forceRefresh: forceRefresh);
            latestRelease = history.FirstOrDefault();
            releaseCards = history.Select((notes, index) =>
                new ViewModels.ReleaseNoteCardViewModel(notes, isLatest: index == 0)).ToArray();
            foreach (var card in releaseCards) card.Update(updateStatus);
            ReleaseNotesHistory.ItemsSource = releaseCards;
            ReleaseNotesStatus.Text = history.Count == 0 ? "No releases published yet." : "";
            ReleaseNotesStatus.Visibility = history.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            UpdateReleaseNotesIndicator();
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or JsonException)
        {
            System.Diagnostics.Trace.TraceWarning("Release notes could not be loaded: {0}", exception);
            ReleaseNotesStatus.Text = exception switch
            {
                JsonException => "The release notes response could not be read. Try Refresh later.",
                HttpRequestException { StatusCode: HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests }
                    => "GitHub has temporarily rate-limited requests. Please try Refresh later.",
                HttpRequestException { StatusCode: not null } => "The release notes service is unavailable. Try Refresh later.",
                _ => "Could not connect to the release notes service. Check your connection and try Refresh."
            };
        }
        finally
        {
            loadingReleaseNotes = false;
            UpdateReleaseRefreshControls();
        }
    }
}
