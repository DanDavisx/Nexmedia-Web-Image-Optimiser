// Calculates output dimensions.

namespace NexMedia.WebImageOptimiser.Core.Processing;

public static class ResizeCalculator
{
    public static ResizeDimensions Calculate(
        double originalWidth,
        double originalHeight,
        ResizeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (originalWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(originalWidth));
        }

        if (originalHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(originalHeight));
        }

        if (settings.Mode == ImageResizeMode.Crop)
        {
            return CalculateCropDimensions(
                originalWidth,
                originalHeight,
                settings);
        }

        int? targetWidth = settings.Bounds.Width;
        int? targetHeight = settings.Bounds.Height;

        double widthScale =
            targetWidth.HasValue
                ? targetWidth.Value / originalWidth
                : double.PositiveInfinity;

        double heightScale =
            targetHeight.HasValue
                ? targetHeight.Value / originalHeight
                : double.PositiveInfinity;

        double scale =
            Math.Min(widthScale, heightScale);

        if (!settings.AllowUpscaling)
        {
            scale = Math.Min(scale, 1.0);
        }

        int calculatedWidth =
            Math.Max(
                1,
                (int)Math.Round(originalWidth * scale));

        int calculatedHeight =
            Math.Max(
                1,
                (int)Math.Round(originalHeight * scale));

        return new ResizeDimensions(
            calculatedWidth,
            calculatedHeight);
    }

    private static ResizeDimensions CalculateCropDimensions(
        double originalWidth,
        double originalHeight,
        ResizeSettings settings)
    {
        if (settings.Bounds.Width is not int targetWidth ||
            settings.Bounds.Height is not int targetHeight)
        {
            throw new ArgumentException(
                "Crop to fill requires both dimensions.",
                nameof(settings));
        }

        double outputScale = settings.AllowUpscaling
            ? 1d
            : Math.Min(
                1d,
                Math.Min(
                    originalWidth / targetWidth,
                    originalHeight / targetHeight));

        return new ResizeDimensions(
            Math.Max(
                1,
                (int)Math.Round(targetWidth * outputScale)),
            Math.Max(
                1,
                (int)Math.Round(targetHeight * outputScale)));
    }
}
