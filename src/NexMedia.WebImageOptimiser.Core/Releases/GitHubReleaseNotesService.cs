using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexMedia.WebImageOptimiser.Core.Releases;

public sealed record ReleaseNotes(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("tag_name")] string? Tag,
    [property: JsonPropertyName("body")] string? Body,
    [property: JsonPropertyName("published_at")] DateTimeOffset? PublishedAt)
{
    [JsonPropertyName("prerelease")]
    public bool IsPrerelease { get; init; }
    [JsonPropertyName("draft")]
    public bool IsDraft { get; init; }
    public bool IsStable => !IsPrerelease && !IsDraft && !string.IsNullOrWhiteSpace(Tag)
        && !Tag.Split('+')[0].Contains('-');
    public bool HasNotes => !string.IsNullOrWhiteSpace(Body);
    public string DisplayName => string.IsNullOrWhiteSpace(Tag)
        ? string.IsNullOrWhiteSpace(Name) ? "Untitled release" : Name
        : string.IsNullOrWhiteSpace(Name) ? $"[{Tag}]" : $"[{Tag}] {Name}";
}

public sealed class GitHubReleaseNotesService(HttpClient client)
{
    private readonly SemaphoreSlim cacheLock = new(1, 1);
    private IReadOnlyList<ReleaseNotes>? cachedHistory;

    public async Task<ReleaseNotes?> GetLatestAsync(CancellationToken cancellationToken = default)
        => (await GetHistoryAsync(cancellationToken)).FirstOrDefault();

    public async Task<IReadOnlyList<ReleaseNotes>> GetHistoryAsync(CancellationToken cancellationToken = default, bool forceRefresh = false)
    {
        await cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && cachedHistory is not null) return cachedHistory;
            var history = new List<ReleaseNotes>();
            for (int page = 1; ; page++)
            {
                var releases = await GetPageAsync(page, cancellationToken);
                history.AddRange(releases.Where(release => release.IsStable));
                if (releases.Length < 100)
                {
                    cachedHistory = Array.AsReadOnly(history.OrderByDescending(release => release.PublishedAt).ToArray());
                    return cachedHistory;
                }
            }
        }
        finally { cacheLock.Release(); }
    }

    private async Task<ReleaseNotes[]> GetPageAsync(int page, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.github.com/repos/DanDavisx/Nexmedia-Web-Image-Optimiser/releases?per_page=100&page={page}");
        request.Headers.UserAgent.ParseAdd("NexMedia-Web-Image-Optimiser/0.1");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return [];
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var releases = await JsonSerializer.DeserializeAsync<ReleaseNotes[]>(stream, cancellationToken: cancellationToken);
        return releases ?? [];
    }
}
