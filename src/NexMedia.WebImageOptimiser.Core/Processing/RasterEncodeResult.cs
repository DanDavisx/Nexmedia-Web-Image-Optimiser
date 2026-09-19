// Represents an encoded raster image in memory, including its format, dimensions, and resulting file size.

using NexMedia.WebImageOptimiser.Core.Configuration;

namespace NexMedia.WebImageOptimiser.Core.Processing;

public sealed record RasterEncodeResult(
    byte[] Data,
    RasterOutputFormat Format,
    int Width,
    int Height)
{
    public long SizeBytes => Data.LongLength;
}
