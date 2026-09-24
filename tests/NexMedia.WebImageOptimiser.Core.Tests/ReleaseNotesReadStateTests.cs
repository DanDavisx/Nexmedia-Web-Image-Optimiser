using NexMedia.WebImageOptimiser.Core.Releases;

namespace NexMedia.WebImageOptimiser.Core.Tests;

[TestClass]
public sealed class ReleaseNotesReadStateTests
{
    [TestMethod]
    public void ViewingReleaseClearsNotificationAcrossLaunchesAndNewReleaseIsUnread()
    {
        string directory = Path.Combine(Path.GetTempPath(), "NexMediaReadStateTests", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "viewed-release.txt");
        try
        {
            var release = new ReleaseNotes("Release", "v1.0.0", "Changes", null);
            var state = new ReleaseNotesReadState(path);
            Assert.IsTrue(state.IsUnread(release));
            state.MarkViewed(release);
            Assert.IsFalse(state.IsUnread(release));

            var restored = new ReleaseNotesReadState(path);
            Assert.IsFalse(restored.IsUnread(release));
            Assert.IsTrue(restored.IsUnread(release with { Tag = "v1.1.0" }));
            Assert.IsFalse(restored.IsUnread(release with { Tag = "v1.2.0", Body = " " }));
            Assert.IsFalse(restored.IsUnread(null));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void FailedPersistenceStillClearsNotificationForCurrentSession()
    {
        string path = Path.GetTempFileName();
        try
        {
            var state = new ReleaseNotesReadState(Path.Combine(path, "viewed-release.txt"));
            var release = new ReleaseNotes("Release", "v1.0.0", "Changes", null);
            state.MarkViewed(release);
            Assert.IsFalse(state.IsUnread(release));
        }
        finally { File.Delete(path); }
    }
}
