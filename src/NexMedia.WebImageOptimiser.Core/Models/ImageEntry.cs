// Represents a single imported image and stores its metadata, including path, format, dimensions, and original file size.

namespace NexMedia.WebImageOptimiser.Core.Models;

public sealed record ImageEntry
{
    public required string FilePath { get; init; }

    public string FileName => Path.GetFileName(FilePath);

    public required ImageFileFormat Format { get; init; }

    public required double Width { get; init; }

    public required double Height { get; init; }

    public required long OriginalSizeBytes { get; init; }
}
