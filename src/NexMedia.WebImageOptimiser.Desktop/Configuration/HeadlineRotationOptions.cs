// The rotating title at the top of the app.

using System.IO;
using System.Text.Json;

namespace NexMedia.WebImageOptimiser.Desktop.Configuration;

internal sealed class HeadlineRotationOptions
{
    private const int DefaultIntervalSeconds = 20;

    private const int DefaultCharacterDelayMilliseconds = 45;

    private const int DefaultCharacterFadeDurationMilliseconds = 120;

    private static readonly string[] DefaultMessages =
    [
        "Let’s make every byte count.",
        "What are we optimising today?",
        "Ready to optimise?",
        "Let’s clean up some images.",
        "Ready when you are.",
        "Let’s make these images web-ready."
    ];

    public int IntervalSeconds { get; init; } =
        DefaultIntervalSeconds;

    public int CharacterDelayMilliseconds { get; init; } =
        DefaultCharacterDelayMilliseconds;

    public int CharacterFadeDurationMilliseconds { get; init; } =
        DefaultCharacterFadeDurationMilliseconds;

    public IReadOnlyList<string> Messages { get; init; } =
        DefaultMessages;

    public static HeadlineRotationOptions Load(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return CreateDefaults();
            }

            string json = File.ReadAllText(path);

            HeadlineRotationOptions? loaded =
                JsonSerializer.Deserialize<HeadlineRotationOptions>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (loaded is null)
            {
                return CreateDefaults();
            }

            string[] messages =
                loaded.Messages?
                    .Where(message =>
                        !string.IsNullOrWhiteSpace(message))
                    .Select(message => message.Trim())
                    .ToArray() ?? [];

            if (messages.Length == 0)
            {
                messages = DefaultMessages;
            }

            return new HeadlineRotationOptions
            {
                IntervalSeconds = Math.Max(1, loaded.IntervalSeconds),
                CharacterDelayMilliseconds =
                    Math.Clamp(
                        loaded.CharacterDelayMilliseconds,
                        0,
                        1000),
                CharacterFadeDurationMilliseconds =
                    Math.Clamp(
                        loaded.CharacterFadeDurationMilliseconds,
                        0,
                        2000),
                Messages = messages
            };
        }
        catch (Exception exception)
            when (exception is IOException or
                  UnauthorizedAccessException or
                  JsonException)
        {
            return CreateDefaults();
        }
    }

    private static HeadlineRotationOptions CreateDefaults() =>
        new()
        {
            Messages = DefaultMessages.ToArray()
        };
}
