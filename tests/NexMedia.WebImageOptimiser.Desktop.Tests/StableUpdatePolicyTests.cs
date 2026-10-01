using NexMedia.WebImageOptimiser.Desktop.Configuration;
using Velopack;

namespace NexMedia.WebImageOptimiser.Desktop.Tests;

[TestClass]
public sealed class StableUpdatePolicyTests
{
    [TestMethod]
    public void GithubSourceExcludesPrereleases()
        => Assert.IsFalse(AppUpdateService.CreateUpdateSource().Prerelease);

    [TestMethod]
    [DataRow("0.1.0", true)]
    [DataRow("1.2.3+build.4", true)]
    [DataRow("0.1.0-dev.5.1", false)]
    [DataRow("1.2.3-alpha.1", false)]
    [DataRow("1.2.3-beta.1", false)]
    [DataRow("1.2.3-rc.1", false)]
    public void DownloadAndInstallPolicyAcceptsOnlyStableVersions(string version, bool allowed)
        => Assert.AreEqual(allowed, AppUpdateService.IsStableUpdate(SemanticVersion.Parse(version)));
}
