using System.Net;
using NexMedia.WebImageOptimiser.Core.Releases;

namespace NexMedia.WebImageOptimiser.Core.Tests;

[TestClass]
public sealed class GitHubReleaseNotesServiceTests
{
    [TestMethod]
    public async Task EmptyReleaseListReturnsEmptyState()
    {
        using var client = new HttpClient(new ResponseHandler(HttpStatusCode.OK, "[]"));
        Assert.IsNull(await new GitHubReleaseNotesService(client).GetLatestAsync());
    }

    [TestMethod]
    public async Task LatestReleaseIncludesPrereleasesAndIgnoresOlderEntries()
    {
        using var client = new HttpClient(new ResponseHandler(HttpStatusCode.OK,
            """[{"tag_name":"v0.2.0","prerelease":true,"body":"Preview changes"},{"tag_name":"v0.1.0","body":"Older changes"}]"""));
        var notes = await new GitHubReleaseNotesService(client).GetLatestAsync();
        Assert.IsNotNull(notes);
        Assert.AreEqual("v0.2.0", notes.Tag);
        Assert.AreEqual("Preview changes", notes.Body);
    }

    [TestMethod]
    public async Task NoPublishedReleaseReturnsEmptyState()
    {
        using var client = new HttpClient(new ResponseHandler(HttpStatusCode.NotFound, "{}"));
        Assert.IsNull(await new GitHubReleaseNotesService(client).GetLatestAsync());
    }

    [TestMethod]
    public async Task PublishedDescriptionIsPreservedAndIndicatesNotesAvailable()
    {
        using var client = new HttpClient(new ResponseHandler(HttpStatusCode.OK,
            """[{"name":"First release","tag_name":"v0.1.0","body":"## Changes\n- Display fixes","published_at":"2026-09-23T12:00:00Z"}]"""));
        var notes = await new GitHubReleaseNotesService(client).GetLatestAsync();
        Assert.IsNotNull(notes);
        Assert.IsTrue(notes.HasNotes);
        Assert.AreEqual("## Changes\n- Display fixes", notes.Body);
        Assert.AreEqual("v0.1.0", notes.Tag);
    }

    [TestMethod]
    public async Task EmptyDescriptionDoesNotIndicateNotesAvailable()
    {
        using var client = new HttpClient(new ResponseHandler(HttpStatusCode.OK,
            """[{"tag_name":"v0.1.0","body":"  "}]"""));
        var notes = await new GitHubReleaseNotesService(client).GetLatestAsync();
        Assert.IsNotNull(notes);
        Assert.IsFalse(notes.HasNotes);
    }

    [TestMethod]
    public async Task RateLimitIsAnErrorRatherThanNoReleases()
    {
        using var client = new HttpClient(new ResponseHandler(HttpStatusCode.Forbidden, "{}"));
        try
        {
            await new GitHubReleaseNotesService(client).GetLatestAsync();
            Assert.Fail("Expected an HTTP error.");
        }
        catch (HttpRequestException exception)
        {
            Assert.AreEqual(HttpStatusCode.Forbidden, exception.StatusCode);
        }
    }

    private sealed class ResponseHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.AreEqual("/repos/DanDavisx/Nexmedia-Web-Image-Optimiser/releases", request.RequestUri!.AbsolutePath);
            Assert.IsTrue(request.Headers.UserAgent.Count > 0);
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }
}
