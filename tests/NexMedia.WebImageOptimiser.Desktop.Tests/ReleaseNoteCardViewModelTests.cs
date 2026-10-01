using System.Reflection;
using NexMedia.WebImageOptimiser.Core.Releases;
using NexMedia.WebImageOptimiser.Desktop.Configuration;
using NexMedia.WebImageOptimiser.Desktop.ViewModels;

namespace NexMedia.WebImageOptimiser.Desktop.Tests;

[TestClass]
public sealed class ReleaseNoteCardViewModelTests
{
    private static string CurrentVersion => typeof(ReleaseNoteCardViewModel).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

    private static ReleaseNoteCardViewModel Card(string tag, bool latest = true)
        => new(new ReleaseNotes("Release", tag, "Changes", null), latest);

    [TestMethod]
    public void LatestReleaseMatchingRunningVersionHidesUpdateButton()
    {
        var card = Card("v" + CurrentVersion);
        Assert.IsTrue(card.IsCurrentVersion);
        Assert.IsFalse(card.ShowUpdateAction);
    }

    [TestMethod]
    public void LatestReleaseWithDifferentVersionShowsCheckForUpdate()
    {
        var card = Card("v9999.0.0");
        Assert.IsFalse(card.IsCurrentVersion);
        Assert.IsTrue(card.ShowUpdateAction);
        Assert.AreEqual("Check for update", card.UpdateActionText);
    }

    [TestMethod]
    public void OlderReleaseDoesNotShowCheckForUpdate()
    {
        var card = Card("v0.0.0", latest: false);
        Assert.IsFalse(card.IsCurrentVersion);
        Assert.IsFalse(card.ShowUpdateAction);
    }

    [TestMethod]
    [DataRow("v1.2.3", "1.2.3", true)]
    [DataRow(" V1.2.3 ", "1.2.3+commit", true)]
    [DataRow("v1.2.3-dev.5", "1.2.3-dev.5+commit", true)]
    [DataRow("v1.2.3-dev.5", "1.2.3-dev.6", false)]
    [DataRow("v1.2.3-dev.5", "1.2.3", false)]
    [DataRow("v1.2.3", "1.2.4", false)]
    [DataRow("invalid", "1.2.3", false)]
    [DataRow(null, "1.2.3", false)]
    public void VersionComparisonHandlesTagsPrereleasesAndBuildMetadata(string? release, string current, bool matches)
        => Assert.AreEqual(matches, ReleaseNoteCardViewModel.VersionsMatch(release, current));

    [TestMethod]
    public void DownloadedUpdateOffersRestartOnMatchingRelease()
    {
        var card = Card("v9999.0.0");
        card.Update(new(AppUpdateState.Ready, "Update ready", "9999.0.0"));
        Assert.IsTrue(card.IsReady);
        Assert.IsTrue(card.ShowUpdateAction);
        Assert.AreEqual("Restart to update", card.UpdateActionText);
    }

    [TestMethod]
    public void DownloadForDifferentReleaseDoesNotOfferRestart()
    {
        var card = Card("v9999.0.0");
        card.Update(new(AppUpdateState.Ready, "Update ready", "9998.0.0"));
        Assert.IsFalse(card.IsReady);
        Assert.AreEqual("Check for update", card.UpdateActionText);
    }

    [TestMethod]
    public void FailedCheckWithoutTargetVersionStillOffersUpdateForMismatch()
    {
        var card = Card("v9999.0.0");
        card.Update(new(AppUpdateState.Failed, "GitHub has temporarily rate-limited requests."));
        Assert.IsTrue(card.ShowUpdateAction);
        Assert.AreEqual("Check for update", card.UpdateActionText);
    }

    [TestMethod]
    public void CooldownDisablesActionAndNotifiesBindingThenAllowsRetry()
    {
        var card = Card("v9999.0.0");
        string? changedProperty = null;
        card.PropertyChanged += (_, args) => changedProperty = args.PropertyName;
        card.SetUpdateActionEnabled(false);
        Assert.IsFalse(card.IsUpdateActionEnabled);
        Assert.AreEqual(nameof(card.IsUpdateActionEnabled), changedProperty);
        card.SetUpdateActionEnabled(true);
        Assert.IsTrue(card.IsUpdateActionEnabled);
    }
}
