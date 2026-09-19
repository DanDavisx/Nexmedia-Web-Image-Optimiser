// Encodes processed raster bitmaps as WebP or PNG without writing them to disk.

using NexMedia.WebImageOptimiser.Core.Configuration;
using SkiaSharp;

namespace NexMedia.WebImageOptimiser.Core.Processing;

public static class RasterEncoder
{
    public static RasterEncodeResult Encode(
        SKBitmap bitmap,
        RasterOutputFormat format,
        int webPQuality = 80)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        if (bitmap.Width <= 0 || bitmap.Height <= 0)
        {
            throw new ArgumentException(
                "The bitmap must contain valid dimensions.",
                nameof(bitmap));
        }

        if (webPQuality is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(webPQuality),
                "WebP quality must be between 0 and 100.");
        }

        SKEncodedImageFormat encodedFormat =
            format switch
            {
                RasterOutputFormat.WebP =>
                    SKEncodedImageFormat.Webp,

                RasterOutputFormat.Png =>
                    SKEncodedImageFormat.Png,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(format))
            };

        using SKImage image =
            SKImage.FromBitmap(bitmap);

        int quality =
            format == RasterOutputFormat.WebP
                ? webPQuality
                : 100;

        using SKData? data =
            image.Encode(
                encodedFormat,
                quality);

        if (data is null)
        {
            throw new InvalidOperationException(
                $"Failed to encode image as {format}.");
        }

        return new RasterEncodeResult(
            data.ToArray(),
            format,
            bitmap.Width,
            bitmap.Height);
    }
}
