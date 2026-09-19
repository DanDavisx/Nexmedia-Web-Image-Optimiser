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
            throw new NotSupportedException(
                "Crop resizing has not been implemented yet.");
        }

        int? targetWidth = settings.Bounds.Width;
        int? targetHeight = settings.Bounds.Height;

        if (!settings.PreserveAspectRatio)
        {
            int width = targetWidth
                ?? (int)Math.Round(originalWidth);

            int height = targetHeight
                ?? (int)Math.Round(originalHeight);

            if (!settings.AllowUpscaling)
            {
                width = Math.Min(
                    width,
                    (int)Math.Round(originalWidth));

                height = Math.Min(
                    height,
                    (int)Math.Round(originalHeight));
            }

            return new ResizeDimensions(
                Math.Max(1, width),
                Math.Max(1, height));
        }

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
}
