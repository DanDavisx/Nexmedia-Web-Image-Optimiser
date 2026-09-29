using System.Diagnostics;
using System.IO;
using Velopack;
using Velopack.Sources;

namespace NexMedia.WebImageOptimiser.Desktop.Configuration;

public enum AppUpdateState { Idle, Checking, Unavailable, Current, Downloading, Ready, Failed }

public sealed record AppUpdateStatus(AppUpdateState State, string Message, string? Version = null);

internal sealed class AppUpdateService
{
    private readonly SemaphoreSlim updateLock = new(1, 1);
    private readonly string lastCheckPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NexMedia", "WebImageOptimiser", "last-update-check.txt");

    public async Task<AppUpdateStatus> CheckAndDownloadAsync(bool manual, IProgress<AppUpdateStatus>? progress = null)
    {
        if (!await updateLock.WaitAsync(0)) return new(AppUpdateState.Checking, "An update check is already running.");
        string? targetVersion = null;
        try
        {
            var manager = new UpdateManager(new GithubSource(
                "https://github.com/DanDavisx/Nexmedia-Web-Image-Optimiser", null, true)); // Temporarily set to true to enable pre releases.
            if (!manager.IsInstalled)
                return new(AppUpdateState.Unavailable, "Automatic updates are available in the installed release build.");
            if (manager.UpdatePendingRestart is { } pending)
                return new(AppUpdateState.Ready, "Update ready. Restart the app to install it.", pending.Version.ToString());

            if (!manual && File.Exists(lastCheckPath)
                && DateTime.UtcNow - File.GetLastWriteTimeUtc(lastCheckPath) < TimeSpan.FromHours(6))
                return new(AppUpdateState.Idle, "Updates are checked automatically every six hours when the app opens.");

            // Throttle unsuccessful checks
            Directory.CreateDirectory(Path.GetDirectoryName(lastCheckPath)!);
            await File.WriteAllTextAsync(lastCheckPath, DateTime.UtcNow.ToString("O"));
            var update = await manager.CheckForUpdatesAsync();
            if (update is null) return new(AppUpdateState.Current, "You're up to date.");
            targetVersion = update.TargetFullRelease.Version.ToString();
            progress?.Report(new(AppUpdateState.Downloading, "Downloading update…", targetVersion));
            await manager.DownloadUpdatesAsync(update, percent =>
                progress?.Report(new(AppUpdateState.Downloading, $"Downloading update… {percent}%", targetVersion)));
            return new(AppUpdateState.Ready, "Update ready. Restart the app to install it.", targetVersion);
        }
        catch (Exception exception)
        {
            Trace.TraceWarning("Update check or download failed: {0}", exception);
            return new(AppUpdateState.Failed, "Could not check or download updates. Try again later; the app is still ready to use.", targetVersion);
        }
        finally
        {
            updateLock.Release();
        }
    }
    public void RestartToApply()
    {
        var manager = new UpdateManager(new GithubSource(
            "https://github.com/DanDavisx/Nexmedia-Web-Image-Optimiser", null, true));
        if (manager.UpdatePendingRestart is not { } pending)
            throw new InvalidOperationException("No downloaded update is ready. Refresh to check again.");
        manager.ApplyUpdatesAndRestart(pending);
    }
}
