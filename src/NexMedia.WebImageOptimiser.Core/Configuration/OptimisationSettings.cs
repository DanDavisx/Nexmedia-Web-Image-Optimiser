namespace NexMedia.WebImageOptimiser.Core.Configuration;

/// <summary>
/// An immutable settings snapshot, ready to assign to an image or group.
/// These are configuration values; image processing is introduced in later milestones.
/// </summary>
public sealed record OptimisationSettings
{
    // KB is decimal throughout the app: 200 KB = 200,000 bytes.
    public const long DefaultTargetSizeBytes = 200_000;

    public OptimisationSettings(
        ResizeBounds bounds,
        RasterOutputFormat outputFormat = RasterOutputFormat.WebP,
        ResizeMode resizeMode = ResizeMode.FitWithin,
        long targetSizeBytes = DefaultTargetSizeBytes,
        int minimumWebPQuality = 60,
        bool allowFurtherDimensionReduction = false)
    {
        ArgumentNullException.ThrowIfNull(bounds);

        if (!Enum.IsDefined(outputFormat))
        {
            throw new ArgumentOutOfRangeException(nameof(outputFormat));
        }

        if (!Enum.IsDefined(resizeMode))
        {
            throw new ArgumentOutOfRangeException(nameof(resizeMode));
        }

        if (resizeMode == ResizeMode.CropToFill && (bounds.Width is null || bounds.Height is null))
        {
            throw new ArgumentException("Crop to fill requires both dimensions.", nameof(bounds));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetSizeBytes);
        ArgumentOutOfRangeException.ThrowIfLessThan(minimumWebPQuality, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minimumWebPQuality, 100);

        Bounds = bounds;
        OutputFormat = outputFormat;
        ResizeMode = resizeMode;
        TargetSizeBytes = targetSizeBytes;
        MinimumWebPQuality = minimumWebPQuality;
        AllowFurtherDimensionReduction = allowFurtherDimensionReduction;
    }

    public ResizeBounds Bounds { get; }
    public RasterOutputFormat OutputFormat { get; }
    public ResizeMode ResizeMode { get; }
    public long TargetSizeBytes { get; }
    /// <summary>Applies only to lossy WebP, never PNG or SVG.</summary>
    public int MinimumWebPQuality { get; }
    public bool AllowFurtherDimensionReduction { get; }
}
