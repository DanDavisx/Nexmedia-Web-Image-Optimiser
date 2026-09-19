// Responsible for decoding raster images and resizing pixel data to output dimensions.

using NexMedia.WebImageOptimiser.Core.Models;
using SkiaSharp;

namespace NexMedia.WebImageOptimiser.Core.Processing;

public static class RasterResizeProcessor
{
    private static readonly SKSamplingOptions ResizeSampling =
        new(SKFilterMode.Linear, SKMipmapMode.Linear);

    private static readonly SKSamplingOptions OrientationSampling =
        new(SKFilterMode.Nearest);

    public static SKBitmap Resize(ImageBatchItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        ResizeDimensions dimensions =
            item.PlannedDimensions
            ?? throw new InvalidOperationException(
                "Resize settings must be applied before processing the image.");

        return Resize(item, dimensions);
    }

    public static SKBitmap Resize(ImageBatchItem item, ResizeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(settings);

        ResizeDimensions dimensions =
            ResizeCalculator.Calculate(item.Width, item.Height, settings);

        return Resize(item, dimensions);
    }

    internal static SKBitmap Resize(
        ImageBatchItem item,
        ResizeDimensions dimensions)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(dimensions);

        if (item.Format == ImageFileFormat.Svg)
        {
            throw new InvalidOperationException(
                "SVG files must use the vector-preserving pipeline.");
        }

        if (dimensions.Width <= 0 || dimensions.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dimensions),
                "Output dimensions must be greater than zero.");
        }

        using SKBitmap decoded =
            Decode(
                item.FilePath,
                out SKEncodedOrigin origin);

        using SKBitmap oriented =
            ApplyOrientation(
                decoded,
                origin);

        return ResizeBitmap(oriented, dimensions);
    }

    private static SKBitmap Decode(
        string filePath,
        out SKEncodedOrigin origin)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "The source image could not be found.",
                filePath);
        }

        using var fileStream =
            File.OpenRead(filePath);

        using var skStream =
            new SKManagedStream(fileStream);

        using SKCodec codec =
            SKCodec.Create(skStream)
            ?? throw new InvalidDataException(
                "The source file could not be decoded.");

        origin = codec.EncodedOrigin;

        var decodeInfo = new SKImageInfo(
            codec.Info.Width,
            codec.Info.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);

        var bitmap =
            new SKBitmap(decodeInfo);

        SKCodecResult result =
            codec.GetPixels(
                decodeInfo,
                bitmap.GetPixels());

        if (result != SKCodecResult.Success)
        {
            bitmap.Dispose();

            throw new InvalidDataException(
                $"Image decoding failed with result: {result}.");
        }

        return bitmap;
    }

    private static SKBitmap ApplyOrientation(
        SKBitmap source,
        SKEncodedOrigin origin)
    {
        bool swapDimensions =
            origin is
                SKEncodedOrigin.LeftTop or
                SKEncodedOrigin.RightTop or
                SKEncodedOrigin.RightBottom or
                SKEncodedOrigin.LeftBottom;

        int outputWidth =
            swapDimensions
                ? source.Height
                : source.Width;

        int outputHeight =
            swapDimensions
                ? source.Width
                : source.Height;

        var output = new SKBitmap(
            outputWidth,
            outputHeight,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);

        using var canvas =
            new SKCanvas(output);

        using var image =
            SKImage.FromBitmap(source);

        SKMatrix matrix =
            CreateOrientationMatrix(
                origin,
                source.Width,
                source.Height);

        canvas.SetMatrix(matrix);

        canvas.DrawImage(
            image,
            0,
            0,
            OrientationSampling);

        return output;
    }

    private static SKMatrix CreateOrientationMatrix(
        SKEncodedOrigin origin,
        int width,
        int height)
    {
        return origin switch
        {
            SKEncodedOrigin.TopRight =>
                new SKMatrix(
                    -1, 0, width,
                    0, 1, 0,
                    0, 0, 1),

            SKEncodedOrigin.BottomRight =>
                new SKMatrix(
                    -1, 0, width,
                    0, -1, height,
                    0, 0, 1),

            SKEncodedOrigin.BottomLeft =>
                new SKMatrix(
                    1, 0, 0,
                    0, -1, height,
                    0, 0, 1),

            SKEncodedOrigin.LeftTop =>
                new SKMatrix(
                    0, 1, 0,
                    1, 0, 0,
                    0, 0, 1),

            SKEncodedOrigin.RightTop =>
                new SKMatrix(
                    0, -1, height,
                    1, 0, 0,
                    0, 0, 1),

            SKEncodedOrigin.RightBottom =>
                new SKMatrix(
                    0, -1, height,
                    -1, 0, width,
                    0, 0, 1),

            SKEncodedOrigin.LeftBottom =>
                new SKMatrix(
                    0, 1, 0,
                    -1, 0, width,
                    0, 0, 1),

            _ => SKMatrix.CreateIdentity()
        };
    }

    private static SKBitmap ResizeBitmap(
        SKBitmap source,
        ResizeDimensions dimensions)
    {
        var output = new SKBitmap(
            dimensions.Width,
            dimensions.Height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul);

        using var canvas =
            new SKCanvas(output);

        using var image =
            SKImage.FromBitmap(source);

        var destination = new SKRect(
            0,
            0,
            dimensions.Width,
            dimensions.Height);

        canvas.DrawImage(
            image,
            destination,
            ResizeSampling);

        return output;
    }
}
