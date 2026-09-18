namespace NexMedia.WebImageOptimiser.Core.Configuration;

/// <summary>Pixel limits. A null axis is unconstrained, as in a maximum-width preset.</summary>
public sealed record ResizeBounds
{
    public ResizeBounds(int? width, int? height)
    {
        if (width is null && height is null)
        {
            throw new ArgumentException("At least one dimension is required.");
        }

        if (width is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
        }

        if (height is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");
        }

        Width = width;
        Height = height;
    }

    public int? Width { get; }
    public int? Height { get; }
}
