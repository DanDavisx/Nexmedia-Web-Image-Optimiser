using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using Velopack;
using Velopack.Sources;

namespace NexMedia.WebImageOptimiser.Desktop.Configuration;

public enum AppUpdateState { Idle, Checking, Unavailable, Current, Downloading, Ready, Failed }

public sealed record AppUpdateStatus(AppUpdateState State, string Message, string? Version = null);

internal sealed class AppUpdateService
{
    internal static GithubSource CreateUpdateSource()
        => new("https://github.com/DanDavisx/Nexmedia-Web-Image-Optimiser", null, false);

    internal static bool IsStableUpdate(SemanticVersion version) => !version.IsPrerelease;

    internal static void ApplyStablePendingUpdateOnStartup()
    {
        try
        {
            var manager = new UpdateManager(CreateUpdateSource());
            if (manager.IsInstalled && manager.UpdatePendingRestart is { } pending && IsStableUpdate(pending.Version))
                manager.ApplyUpdatesAndRestart(pending);
        }
        catch (Exception exception)
        {
            Trace.TraceWarning("Pending stable update could not be applied: {0}", exception);
        }
    }

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
            var manager = new UpdateManager(CreateUpdateSource());
            if (!manager.IsInstalled)
                return new(AppUpdateState.Unavailable, "Automatic updates are available in the installed release build.");
            if (manager.UpdatePendingRestart is { } pending && IsStableUpdate(pending.Version))
                return new(AppUpdateState.Ready, "Update ready. Restart the app to install it.", pending.Version.ToString());

            if (!manual && File.Exists(lastCheckPath)
                && DateTime.UtcNow - File.GetLastWriteTimeUtc(lastCheckPath) < TimeSpan.FromHours(6))
                return new(AppUpdateState.Idle, "Updates are checked automatically every six hours when the app opens.");

            // Throttle unsuccessful checks
            Directory.CreateDirectory(Path.GetDirectoryName(lastCheckPath)!);
            await File.WriteAllTextAsync(lastCheckPath, DateTime.UtcNow.ToString("O"));
            var update = await manager.CheckForUpdatesAsync();
            if (update is null || !IsStableUpdate(update.TargetFullRelease.Version))
                return new(AppUpdateState.Current, "You're up to date with stable releases.");
            targetVersion = update.TargetFullRelease.Version.ToString();
            progress?.Report(new(AppUpdateState.Downloading, "Downloading update…", targetVersion));
            await manager.DownloadUpdatesAsync(update, percent =>
                progress?.Report(new(AppUpdateState.Downloading, $"Downloading update… {percent}%", targetVersion)));
            return new(AppUpdateState.Ready, "Update ready. Restart the app to install it.", targetVersion);
        }
        catch (Exception exception)
        {
            Trace.TraceWarning("Update check or download failed: {0}", exception);
            for (Exception? cause = exception; cause is not null; cause = cause.InnerException)
            {
                if (cause is HttpRequestException { StatusCode: HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests })
                    return new(AppUpdateState.Failed, "GitHub has temporarily rate-limited requests. Please try checking for updates later.", targetVersion);
            }
            return new(AppUpdateState.Failed, "Could not check or download updates. Try again later; the app is still ready to use.", targetVersion);
        }
        finally
        {
            updateLock.Release();
        }
    }
    public void RestartToApply()
    {
        var manager = new UpdateManager(CreateUpdateSource());
        if (manager.UpdatePendingRestart is not { } pending || !IsStableUpdate(pending.Version))
            throw new InvalidOperationException("No stable downloaded update is ready. Refresh to check again.");
        manager.ApplyUpdatesAndRestart(pending);
    }
}
