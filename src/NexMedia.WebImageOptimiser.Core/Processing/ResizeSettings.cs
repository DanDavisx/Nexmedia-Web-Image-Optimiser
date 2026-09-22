// Defines the requested bounds and resizing rules applied to an image.

using NexMedia.WebImageOptimiser.Core.Configuration;

namespace NexMedia.WebImageOptimiser.Core.Processing;

public sealed record ResizeSettings
{
    public required ResizeBounds Bounds { get; init; }

    public bool AllowUpscaling { get; init; } = false;

    public ImageResizeMode Mode { get; init; } = ImageResizeMode.Fit;
}
