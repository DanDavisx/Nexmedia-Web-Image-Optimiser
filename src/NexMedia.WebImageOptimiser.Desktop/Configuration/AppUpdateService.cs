using System.Diagnostics;
using System.IO;
using Velopack;
using Velopack.Sources;

namespace NexMedia.WebImageOptimiser.Desktop.Configuration;

internal sealed class AppUpdateService
{
    private readonly SemaphoreSlim updateLock = new(1, 1);
    private readonly string lastCheckPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NexMedia", "WebImageOptimiser", "last-update-check.txt");

    public async Task<string> CheckAndDownloadAsync(bool manual)
    {
        if (!await updateLock.WaitAsync(0)) return "An update check is already running.";
        try
        {
            var manager = new UpdateManager(new GithubSource(
                "https://github.com/DanDavisx/Nexmedia-Web-Image-Optimiser", null, true)); // Temporarily set to true to enable pre releases.
            if (!manager.IsInstalled)
                return "Automatic updates are available in the installed release build.";
            if (manager.UpdatePendingRestart is not null)
                return "Update ready. Close and reopen the app to install it.";

            if (!manual && File.Exists(lastCheckPath)
                && DateTime.UtcNow - File.GetLastWriteTimeUtc(lastCheckPath) < TimeSpan.FromHours(6))
                return "Updates are checked automatically every six hours when the app opens.";

            // Throttle unsuccessful checks too, including when no release exists yet.
            Directory.CreateDirectory(Path.GetDirectoryName(lastCheckPath)!);
            await File.WriteAllTextAsync(lastCheckPath, DateTime.UtcNow.ToString("O"));
            var update = await manager.CheckForUpdatesAsync();
            if (update is null) return "You're up to date.";
            await manager.DownloadUpdatesAsync(update);
            return "Update ready. Close and reopen the app to install it.";
        }
        catch (Exception exception)
        {
            Trace.TraceWarning("Update check or download failed: {0}", exception);
            return "Could not check or download updates. Try again later; the app is still ready to use.";
        }
        finally
        {
            updateLock.Release();
        }
    }
}
