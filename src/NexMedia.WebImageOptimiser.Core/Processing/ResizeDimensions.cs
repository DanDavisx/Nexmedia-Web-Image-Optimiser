// Represents the calculated output width and height for an image after applying resize rules.

namespace NexMedia.WebImageOptimiser.Core.Processing;

public sealed record ResizeDimensions(
    int Width,
    int Height);
