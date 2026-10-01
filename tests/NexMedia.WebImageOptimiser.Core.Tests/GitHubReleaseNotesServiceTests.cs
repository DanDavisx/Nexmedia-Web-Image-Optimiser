using System.Net;
using NexMedia.WebImageOptimiser.Core.Releases;

namespace NexMedia.WebImageOptimiser.Core.Tests;

[TestClass]
public sealed class GitHubReleaseNotesServiceTests
{
    [TestMethod]
    public async Task HistorySortsByPublicationDateNewestFirstWithUndatedReleasesLast()
    {
        using var client = new HttpClient(new ResponseHandler(HttpStatusCode.OK,
            """[{"tag_name":"undated"},{"tag_name":"older","published_at":"2026-09-20T12:00:00Z"},{"tag_name":"newest","published_at":"2026-09-24T12:00:00Z"}]"""));
        var history = await new GitHubReleaseNotesService(client).GetHistoryAsync();
        CollectionAssert.AreEqual(new[] { "newest", "older", "undated" }, history.Select(notes => notes.Tag).ToArray());
    }

    [TestMethod]
    public async Task HistoryPreservesStableReleasesIncludingThoseWithoutNotes()
    {
        using var client = new HttpClient(new ResponseHandler(HttpStatusCode.OK,
            """[{"tag_name":"v0.3.0","prerelease":true,"body":"Preview"},{"tag_name":"v0.2.0","body":""},{"tag_name":"v0.1.0","body":"First"}]"""));
        var history = await new GitHubReleaseNotesService(client).GetHistoryAsync();
        CollectionAssert.AreEqual(new[] { "v0.2.0", "v0.1.0" }, history.Select(notes => notes.Tag).ToArray());
        Assert.IsFalse(history[0].HasNotes);
        Assert.AreEqual("First", history[1].Body);
    }

    [TestMethod]
    public async Task HistoryLoadsBeyondFirstPage()
    {
        using var handler = new PagedResponseHandler();
        using var client = new HttpClient(handler);
        var history = await new GitHubReleaseNotesService(client).GetHistoryAsync();
        Assert.AreEqual(101, history.Count);
        Assert.AreEqual("oldest", history[100].Tag);
        Assert.AreEqual(2, handler.RequestCount);
    }

    [TestMethod]
    public async Task EmptyHistoryReturnsEmptyList()
    {
        using var client = new HttpClient(new ResponseHandler(HttpStatusCode.OK, "[]"));
        Assert.AreEqual(0, (await new GitHubReleaseNotesService(client).GetHistoryAsync()).Count);
    }

    private sealed class PagedResponseHandler(bool devFirstPage = false) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            Assert.AreEqual($"?per_page=100&page={RequestCount}", request.RequestUri!.Query);
            Assert.IsTrue(RequestCount <= 2);
            string body = RequestCount == 1
                ? "[" + string.Join(",", Enumerable.Range(0, 100).Select(index => $"{{\"tag_name\":\"v{index}\",\"prerelease\":{(devFirstPage ? "true" : "false")}}}")) + "]"
                : """[{"tag_name":"oldest","body":"Original release"}]""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }

    [TestMethod]
    public async Task EmptyReleaseListReturnsEmptyState()
    {
        using var client = new HttpClient(new ResponseHandler(HttpStatusCode.OK, "[]"));
        Assert.IsNull(await new GitHubReleaseNotesService(client).GetLatestAsync());
    }

    [TestMethod]
    public async Task LatestReleaseSkipsPrereleases()
    {
        using var client = new HttpClient(new ResponseHandler(HttpStatusCode.OK,
            """[{"tag_name":"v0.2.0","prerelease":true,"body":"Preview changes"},{"tag_name":"v0.1.0","body":"Older changes"}]"""));
        var notes = await new GitHubReleaseNotesService(client).GetLatestAsync();
        Assert.IsNotNull(notes);
        Assert.AreEqual("v0.1.0", notes.Tag);
        Assert.AreEqual("Older changes", notes.Body);
    }

    [TestMethod]
    public async Task HistoryExcludesDevTagsEvenIfGithubDoesNotMarkThemAsPrereleases()
    {
        using var client = new HttpClient(new ResponseHandler(HttpStatusCode.OK,
            """[{"tag_name":"v2.0.0-dev.1","prerelease":false},{"tag_name":"v2.0.0-rc.1"},{"tag_name":"v2.0.0","draft":true},{"tag_name":"v1.0.0+build.1"}]"""));
        var service = new GitHubReleaseNotesService(client);
        var history = await service.GetHistoryAsync();
        Assert.AreEqual(1, history.Count);
        Assert.AreEqual("v1.0.0+build.1", (await service.GetLatestAsync())?.Tag);
    }

    [TestMethod]
    public async Task OnlyDevReleasesReturnsNoStableRelease()
    {
        using var client = new HttpClient(new ResponseHandler(HttpStatusCode.OK,
            """[{"tag_name":"v1.0.0-dev.1"},{"tag_name":"v1.0.0","prerelease":true}]"""));
        Assert.IsNull(await new GitHubReleaseNotesService(client).GetLatestAsync());
    }

    [TestMethod]
    public async Task FullPageOfDevReleasesStillLoadsStableReleaseOnNextPage()
    {
        using var handler = new PagedResponseHandler(devFirstPage: true);
        using var client = new HttpClient(handler);
        var history = await new GitHubReleaseNotesService(client).GetHistoryAsync();
        Assert.AreEqual(1, history.Count);
        Assert.AreEqual("oldest", history[0].Tag);
        Assert.AreEqual(2, handler.RequestCount);
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
    [DataRow(HttpStatusCode.Forbidden)]
    [DataRow(HttpStatusCode.TooManyRequests)]
    public async Task RateLimitIsAnErrorRatherThanNoReleases(HttpStatusCode status)
    {
        using var client = new HttpClient(new ResponseHandler(status, "{}"));
        try
        {
            await new GitHubReleaseNotesService(client).GetLatestAsync();
            Assert.Fail("Expected an HTTP error.");
        }
        catch (HttpRequestException exception)
        {
            Assert.AreEqual(status, exception.StatusCode);
        }
    }

    [TestMethod]
    public async Task SuccessfulHistoryIsCachedUntilExplicitRefresh()
    {
        using var handler = new ResponseHandler(HttpStatusCode.OK, """[{"tag_name":"v0.1.0"}]""");
        using var client = new HttpClient(handler);
        var service = new GitHubReleaseNotesService(client);
        await service.GetHistoryAsync();
        await service.GetLatestAsync();
        await service.GetHistoryAsync();
        Assert.AreEqual(1, handler.RequestCount);
        await service.GetHistoryAsync(forceRefresh: true);
        Assert.AreEqual(2, handler.RequestCount);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.Forbidden)]
    [DataRow(HttpStatusCode.TooManyRequests)]
    public async Task FailedRefreshPreservesSuccessfulSessionCache(HttpStatusCode status)
    {
        using var handler = new ResponseHandler(HttpStatusCode.OK, """[{"tag_name":"v0.1.0"}]""");
        using var client = new HttpClient(handler);
        var service = new GitHubReleaseNotesService(client);
        await service.GetHistoryAsync();
        handler.Status = status;
        try
        {
            await service.GetHistoryAsync(forceRefresh: true);
            Assert.Fail("Expected a rate-limit error.");
        }
        catch (HttpRequestException exception)
        {
            Assert.AreEqual(status, exception.StatusCode);
        }
        Assert.AreEqual("v0.1.0", (await service.GetLatestAsync())?.Tag);
        Assert.AreEqual(2, handler.RequestCount);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.Forbidden)]
    [DataRow(HttpStatusCode.TooManyRequests)]
    public async Task FailedRequestsAreNotCached(HttpStatusCode status)
    {
        using var handler = new ResponseHandler(status, "[]");
        using var client = new HttpClient(handler);
        var service = new GitHubReleaseNotesService(client);
        try
        {
            await service.GetHistoryAsync();
            Assert.Fail("Expected a rate-limit error.");
        }
        catch (HttpRequestException) { }
        handler.Status = HttpStatusCode.OK;
        Assert.AreEqual(0, (await service.GetHistoryAsync()).Count);
        Assert.AreEqual(2, handler.RequestCount);
    }

    private sealed class ResponseHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpStatusCode Status { get; set; } = status;
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            Assert.AreEqual("/repos/DanDavisx/Nexmedia-Web-Image-Optimiser/releases", request.RequestUri!.AbsolutePath);
            Assert.IsTrue(request.Headers.UserAgent.Count > 0);
            return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(body) });
        }
    }
}
