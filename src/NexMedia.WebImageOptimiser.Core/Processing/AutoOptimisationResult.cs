// Represents the result of automatic raster optimisation, including encoded data, chosen quality, and whether the size target was achieved.

using NexMedia.WebImageOptimiser.Core.Configuration;

namespace NexMedia.WebImageOptimiser.Core.Processing;

public sealed record AutoOptimisationResult(
    byte[] Data,
    RasterOutputFormat Format,
    int Width,
    int Height,
    long SizeBytes,
    int? Quality,
    long TargetSizeBytes,
    bool TargetMet,
    bool DimensionsReduced);
