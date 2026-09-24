using NexMedia.WebImageOptimiser.Core.Releases;

namespace NexMedia.WebImageOptimiser.Desktop.ViewModels;

public sealed record ReleaseNoteCardViewModel(ReleaseNotes Notes, bool IsLatest);
