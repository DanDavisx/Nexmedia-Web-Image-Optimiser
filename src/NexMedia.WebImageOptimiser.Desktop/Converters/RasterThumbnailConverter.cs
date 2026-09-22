// Responsible for resizing and optimising the small image thumbnails.

using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using NexMedia.WebImageOptimiser.Core.Configuration;
using NexMedia.WebImageOptimiser.Core.Processing;
using SkiaSharp;

namespace NexMedia.WebImageOptimiser.Desktop.Converters;

internal sealed class RasterThumbnailConverter : IValueConverter
{
    private static readonly ResizeSettings ThumbnailSettings =
        new()
        {
            Bounds = new ResizeBounds(360, 220),
            Mode = ImageResizeMode.Fit,
            AllowUpscaling = false
        };

    public object? Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (value is not ImageBatchItem item)
        {
            return null;
        }

        try
        {
            using SKBitmap thumbnail =
                RasterResizeProcessor.Resize(
                    item,
                    ThumbnailSettings);

            using SKImage image =
                SKImage.FromBitmap(thumbnail);
            using SKData encoded =
                image.Encode(
                    SKEncodedImageFormat.Png,
                    90);
            using var stream =
                new MemoryStream(encoded.ToArray());

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }
        catch (Exception exception)
            when (exception is IOException or
                  UnauthorizedAccessException or
                  InvalidOperationException)
        {
            return null;
        }
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture) =>
        throw new NotSupportedException();
}
