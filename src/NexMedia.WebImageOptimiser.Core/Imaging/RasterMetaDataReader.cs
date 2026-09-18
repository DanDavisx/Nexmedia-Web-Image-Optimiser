// Reads metadata from JPEG, PNG, and WebP files using SkiaSharp, including format, dimensions, file size, and orientation-aware dimensions.

using NexMedia.WebImageOptimiser.Core.Models;
using SkiaSharp;

namespace NexMedia.WebImageOptimiser.Core.Imaging;
public static class RasterMetadataReader
{
    public static ImageEntry Read(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "A file path must be provided.",
                nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "The image file could not be found.",
                filePath);
        }

        using var stream = File.OpenRead(filePath);
        using var managedStream = new SKManagedStream(stream);
        using var codec = SKCodec.Create(managedStream);

        if (codec is null)
        {
            throw new InvalidDataException(
                "The file is not a valid supported raster image.");
        }

        ImageFileFormat format = GetFormat(codec.EncodedFormat);

        int width = codec.Info.Width;
        int height = codec.Info.Height;

        if (RequiresDimensionSwap(codec.EncodedOrigin))
        {
            (width, height) = (height, width);
        }

        var fileInfo = new FileInfo(filePath);

        return new ImageEntry
        {
            FilePath = fileInfo.FullName,
            Format = format,
            Width = width,
            Height = height,
            OriginalSizeBytes = fileInfo.Length
        };
    }

    private static ImageFileFormat GetFormat(
        SKEncodedImageFormat format)
    {
        return format switch
        {
            SKEncodedImageFormat.Jpeg => ImageFileFormat.Jpeg,
            SKEncodedImageFormat.Png => ImageFileFormat.Png,
            SKEncodedImageFormat.Webp => ImageFileFormat.WebP,

            _ => throw new InvalidDataException(
                $"Unsupported raster format: {format}.")
        };
    }

    private static bool RequiresDimensionSwap(
        SKEncodedOrigin origin)
    {
        return origin is
            SKEncodedOrigin.LeftTop or
            SKEncodedOrigin.RightTop or
            SKEncodedOrigin.RightBottom or
            SKEncodedOrigin.LeftBottom;
    }
}
