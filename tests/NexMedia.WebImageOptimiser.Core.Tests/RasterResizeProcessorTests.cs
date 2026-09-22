// Verifies that raster images are decoded and resized to their planned dimensions without modifying the source file.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexMedia.WebImageOptimiser.Core.Configuration;
using NexMedia.WebImageOptimiser.Core.Models;
using NexMedia.WebImageOptimiser.Core.Processing;
using SkiaSharp;

namespace NexMedia.WebImageOptimiser.Core.Tests;

[TestClass]
public class RasterResizeProcessorTests
{
    private string _testDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(
            Path.GetTempPath(),
            "NexMedia.WebImageOptimiser.Tests",
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(_testDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(
                _testDirectory,
                recursive: true);
        }
    }

    [TestMethod]
    public void Resize_ProducesPlannedDimensions()
    {
        string filePath =
            CreateRasterImage(
                "photo.jpg",
                SKEncodedImageFormat.Jpeg,
                800,
                600);

        var entry = new ImageEntry
        {
            FilePath = filePath,
            Format = ImageFileFormat.Jpeg,
            Width = 800,
            Height = 600,
            OriginalSizeBytes =
                new FileInfo(filePath).Length
        };

        var item =
            new ImageBatchItem(entry);

        item.ApplyResizeSettings(
            new ResizeSettings
            {
                Bounds =
                    new ResizeBounds(
                        400,
                        400)
            });

        using SKBitmap result =
            RasterResizeProcessor.Resize(item);

        Assert.AreEqual(
            400,
            result.Width);

        Assert.AreEqual(
            300,
            result.Height);
    }

    [TestMethod]
    public void Resize_WidthOnlyPreset_ProducesExpectedDimensions()
    {
        string filePath =
            CreateRasterImage(
                "photo.png",
                SKEncodedImageFormat.Png,
                1000,
                500);

        var entry = new ImageEntry
        {
            FilePath = filePath,
            Format = ImageFileFormat.Png,
            Width = 1000,
            Height = 500,
            OriginalSizeBytes =
                new FileInfo(filePath).Length
        };

        var item =
            new ImageBatchItem(entry);

        item.ApplyResizeSettings(
            new ResizeSettings
            {
                Bounds =
                    new ResizeBounds(
                        500,
                        null)
            });

        using SKBitmap result =
            RasterResizeProcessor.Resize(item);

        Assert.AreEqual(
            500,
            result.Width);

        Assert.AreEqual(
            250,
            result.Height);
    }

    [TestMethod]
    public void Resize_DoesNotModifySourceFile()
    {
        string filePath =
            CreateRasterImage(
                "source.webp",
                SKEncodedImageFormat.Webp,
                800,
                600);

        byte[] originalBytes =
            File.ReadAllBytes(filePath);

        var entry = new ImageEntry
        {
            FilePath = filePath,
            Format = ImageFileFormat.WebP,
            Width = 800,
            Height = 600,
            OriginalSizeBytes =
                originalBytes.Length
        };

        var item =
            new ImageBatchItem(entry);

        item.ApplyResizeSettings(
            new ResizeSettings
            {
                Bounds =
                    new ResizeBounds(
                        400,
                        300)
            });

        using SKBitmap result =
            RasterResizeProcessor.Resize(item);

        byte[] bytesAfterProcessing =
            File.ReadAllBytes(filePath);

        CollectionAssert.AreEqual(
            originalBytes,
            bytesAfterProcessing);
    }

    [TestMethod]
    public void Resize_CropMode_CropsFromCentreWithoutStretching()
    {
        string filePath =
            Path.Combine(
                _testDirectory,
                "crop-source.png");

        using (var source =
               new SKBitmap(
                   600,
                   400,
                   SKColorType.Rgba8888,
                   SKAlphaType.Premul))
        {
            source.Erase(SKColors.Lime);

            using var canvas =
                new SKCanvas(source);
            using var outsidePaint =
                new SKPaint
                {
                    Color = SKColors.Magenta
                };

            canvas.DrawRect(
                new SKRect(0, 0, 100, 400),
                outsidePaint);
            canvas.DrawRect(
                new SKRect(500, 0, 600, 400),
                outsidePaint);

            using var image =
                SKImage.FromBitmap(source);
            using SKData encoded =
                image.Encode(
                    SKEncodedImageFormat.Png,
                    100);
            using var output =
                File.Create(filePath);
            encoded.SaveTo(output);
        }

        var item =
            new ImageBatchItem(
                new ImageEntry
                {
                    FilePath = filePath,
                    Format = ImageFileFormat.Png,
                    Width = 600,
                    Height = 400,
                    OriginalSizeBytes =
                        new FileInfo(filePath).Length
                });

        item.ApplyResizeSettings(
            new ResizeSettings
            {
                Bounds = new ResizeBounds(200, 200),
                Mode = ImageResizeMode.Crop
            });

        using SKBitmap result =
            RasterResizeProcessor.Resize(item);

        Assert.AreEqual(200, result.Width);
        Assert.AreEqual(200, result.Height);
        Assert.AreEqual(SKColors.Lime, result.GetPixel(0, 100));
        Assert.AreEqual(SKColors.Lime, result.GetPixel(199, 100));
        Assert.AreEqual(SKColors.Lime, result.GetPixel(100, 100));
    }

    [TestMethod]
    public void Resize_WithoutPlan_ThrowsException()
    {
        string filePath =
            CreateRasterImage(
                "photo.jpg",
                SKEncodedImageFormat.Jpeg,
                800,
                600);

        var entry = new ImageEntry
        {
            FilePath = filePath,
            Format = ImageFileFormat.Jpeg,
            Width = 800,
            Height = 600,
            OriginalSizeBytes =
                new FileInfo(filePath).Length
        };

        var item =
            new ImageBatchItem(entry);

        Assert.ThrowsExactly<InvalidOperationException>(
            () =>
            {
                using SKBitmap result =
                    RasterResizeProcessor.Resize(item);
            });
    }

    private string CreateRasterImage(
        string fileName,
        SKEncodedImageFormat format,
        int width,
        int height)
    {
        string filePath =
            Path.Combine(
                _testDirectory,
                fileName);

        using var bitmap =
            new SKBitmap(
                width,
                height);

        using var image =
            SKImage.FromBitmap(bitmap);

        using SKData data =
            image.Encode(
                format,
                90);

        using var output =
            File.Create(filePath);

        data.SaveTo(output);

        return filePath;
    }
}
