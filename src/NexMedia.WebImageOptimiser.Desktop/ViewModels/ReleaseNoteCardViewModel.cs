using System.ComponentModel;
using System.Reflection;
using NexMedia.WebImageOptimiser.Core.Releases;
using NexMedia.WebImageOptimiser.Desktop.Configuration;
using Velopack;

namespace NexMedia.WebImageOptimiser.Desktop.ViewModels;

public sealed class ReleaseNoteCardViewModel(ReleaseNotes notes, bool isLatest) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public ReleaseNotes Notes { get; } = notes;
    public bool IsLatest { get; } = isLatest;
    public bool IsCurrentVersion => VersionsMatch(Notes.Tag,
        typeof(ReleaseNoteCardViewModel).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
    private AppUpdateStatus status = new(AppUpdateState.Idle, "");
    private bool IsTarget => VersionsMatch(Notes.Tag, status.Version);
    public string UpdateMessage => IsTarget || IsLatest ? status.Message : "";
    public bool ShowUpdateAction => IsTarget && status.State is AppUpdateState.Ready or AppUpdateState.Failed;
    public string UpdateActionText => status.State == AppUpdateState.Ready ? "Restart to update" : "Retry update";
    public bool IsReady => IsTarget && status.State == AppUpdateState.Ready;

    public void Update(AppUpdateStatus value)
    {
        status = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }

    public static bool VersionsMatch(string? left, string? right)
        => SemanticVersion.TryParse(left?.Trim().TrimStart('v', 'V'), out var a)
        && SemanticVersion.TryParse(right?.Trim().TrimStart('v', 'V'), out var b)
        && a.CompareTo(b) == 0;
}
