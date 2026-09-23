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
    public bool HasNotes => !string.IsNullOrWhiteSpace(Body);
}

public sealed class GitHubReleaseNotesService(HttpClient client)
{
    public async Task<ReleaseNotes?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get,
            "https://api.github.com/repos/DanDavisx/Nexmedia-Web-Image-Optimiser/releases"); // Remove "/latest" from the end for testing
        request.Headers.UserAgent.ParseAdd("NexMedia-Web-Image-Optimiser/0.1");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<ReleaseNotes>(stream, cancellationToken: cancellationToken);
    }
}
