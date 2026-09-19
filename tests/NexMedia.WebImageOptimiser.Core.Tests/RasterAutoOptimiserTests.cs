// Verifies automatic WEBP quality optimisation functionality.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexMedia.WebImageOptimiser.Core.Configuration;
using NexMedia.WebImageOptimiser.Core.Models;
using NexMedia.WebImageOptimiser.Core.Processing;
using SkiaSharp;

namespace NexMedia.WebImageOptimiser.Core.Tests;

[TestClass]
public class RasterAutoOptimiserTests
{
    private string _testDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _testDirectory = Path.Combine(
            Path.GetTempPath(),
            "NexMedia.WebImageOptimiser.Tests",
            Guid.NewGuid().ToString());

        Directory.CreateDirectory(
            _testDirectory);
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
    public void Optimise_WebP_ReturnsValidResult()
    {
        ImageBatchItem item =
            CreateBatchItem(
                1200,
                800);

        var settings =
            new OptimisationSettings(
                new ResizeBounds(800, null),
                RasterOutputFormat.WebP,
                targetSizeBytes: 200_000,
                minimumWebPQuality: 60);

        AutoOptimisationResult result =
            RasterAutoOptimiser.Optimise(
                item,
                settings);

        Assert.AreEqual(
            RasterOutputFormat.WebP,
            result.Format);

        Assert.AreEqual(
            800,
            result.Width);

        Assert.AreEqual(
            533,
            result.Height);

        Assert.IsNotNull(
            result.Quality);

        Assert.IsTrue(
            result.Quality >= 60);

        Assert.IsTrue(
            result.Quality <= 100);

        Assert.IsTrue(
            result.SizeBytes > 0);
    }

    [TestMethod]
    public void Optimise_SmallWebP_UsesMaximumQuality()
    {
        ImageBatchItem item =
            CreateBatchItem(
                200,
                100);

        var settings =
            new OptimisationSettings(
                new ResizeBounds(200, null),
                RasterOutputFormat.WebP,
                targetSizeBytes: 1_000_000,
                minimumWebPQuality: 60);

        AutoOptimisationResult result =
            RasterAutoOptimiser.Optimise(
                item,
                settings);

        Assert.AreEqual(
            100,
            result.Quality);

        Assert.IsTrue(
            result.TargetMet);
    }

    [TestMethod]
    public void Optimise_WebP_NeverDropsBelowMinimumQuality()
    {
        ImageBatchItem item =
            CreateBatchItem(
                1200,
                800);

        var settings =
            new OptimisationSettings(
                new ResizeBounds(1200, null),
                RasterOutputFormat.WebP,
                targetSizeBytes: 1,
                minimumWebPQuality: 60);

        AutoOptimisationResult result =
            RasterAutoOptimiser.Optimise(
                item,
                settings);

        Assert.AreEqual(
            60,
            result.Quality);

        Assert.IsFalse(
            result.TargetMet);
    }

    [TestMethod]
    public void Optimise_WebP_CanReduceDimensionsWhenTargetCannotBeMetAtMinimumQuality()
    {
        ImageBatchItem item =
            CreateBatchItem(
                160,
                100);

        OptimisationSettings reductionSettings =
            new(
                new ResizeBounds(160, 100),
                RasterOutputFormat.WebP,
                targetSizeBytes: 1,
                minimumWebPQuality: 60,
                allowFurtherDimensionReduction: true);

        AutoOptimisationResult result =
            RasterAutoOptimiser.Optimise(
                item,
                reductionSettings);

        Assert.IsTrue(
            result.DimensionsReduced);

        Assert.IsTrue(
            result.Width < 160);

        Assert.IsTrue(
            result.Height < 100);

        Assert.IsTrue(
            result.Quality >= 60);
    }

    [TestMethod]
    public void Optimise_Png_UsesLosslessEncoding()
    {
        ImageBatchItem item =
            CreateBatchItem(
                800,
                600);

        var settings =
            new OptimisationSettings(
                new ResizeBounds(400, null),
                RasterOutputFormat.Png,
                targetSizeBytes: 200_000);

        AutoOptimisationResult result =
            RasterAutoOptimiser.Optimise(
                item,
                settings);

        Assert.AreEqual(
            RasterOutputFormat.Png,
            result.Format);

        Assert.IsNull(
            result.Quality);

        Assert.AreEqual(
            400,
            result.Width);

        Assert.AreEqual(
            300,
            result.Height);
    }

    private ImageBatchItem CreateBatchItem(
        int width,
        int height)
    {
        string filePath =
            Path.Combine(
                _testDirectory,
                $"{Guid.NewGuid()}.png");

        using var bitmap =
            new SKBitmap(
                width,
                height);

        FillWithTestPattern(bitmap);

        using SKImage image =
            SKImage.FromBitmap(bitmap);

        using SKData data =
            image.Encode(
                SKEncodedImageFormat.Png,
                100);

        using (FileStream output =
               File.Create(filePath))
        {
            data.SaveTo(output);
        }

        var entry =
            new ImageEntry
            {
                FilePath = filePath,
                Format = ImageFileFormat.Png,
                Width = width,
                Height = height,
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
                        width,
                        height)
            });

        return item;
    }

    private static void FillWithTestPattern(
        SKBitmap bitmap)
    {
        for (int y = 0;
             y < bitmap.Height;
             y++)
        {
            for (int x = 0;
                 x < bitmap.Width;
                 x++)
            {
                bitmap.SetPixel(
                    x,
                    y,
                    new SKColor(
                        (byte)(x % 256),
                        (byte)(y % 256),
                        (byte)((x + y) % 256)));
            }
        }
    }
}
