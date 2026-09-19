// Tests resize calculations for landscape, portrait, no-upscaling, exact-fit, and invalid inputs.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexMedia.WebImageOptimiser.Core.Configuration;
using NexMedia.WebImageOptimiser.Core.Processing;

namespace NexMedia.WebImageOptimiser.Core.Tests;

[TestClass]
public class ResizeCalculatorTests
{
    [TestMethod]
    public void Calculate_LandscapeImage_FitsWithinTarget()
    {
        var settings = new ResizeSettings
        {
            Bounds = new ResizeBounds(1920, 1080)
        };

        ResizeDimensions result =
            ResizeCalculator.Calculate(
                4000,
                3000,
                settings);

        Assert.AreEqual(1440, result.Width);
        Assert.AreEqual(1080, result.Height);
    }

    [TestMethod]
    public void Calculate_PortraitImage_FitsWithinTarget()
    {
        var settings = new ResizeSettings
        {
            Bounds = new ResizeBounds(1920, 1080)
        };

        ResizeDimensions result =
            ResizeCalculator.Calculate(
                3000,
                4000,
                settings);

        Assert.AreEqual(810, result.Width);
        Assert.AreEqual(1080, result.Height);
    }

    [TestMethod]
    public void Calculate_SmallImage_DoesNotUpscaleByDefault()
    {
        var settings = new ResizeSettings
        {
            Bounds = new ResizeBounds(1920, 1080)
        };

        ResizeDimensions result =
            ResizeCalculator.Calculate(
                640,
                480,
                settings);

        Assert.AreEqual(640, result.Width);
        Assert.AreEqual(480, result.Height);
    }

    [TestMethod]
    public void Calculate_SmallImage_CanUpscaleWhenEnabled()
    {
        var settings = new ResizeSettings
        {
            Bounds = new ResizeBounds(1920, 1080),
            AllowUpscaling = true
        };

        ResizeDimensions result =
            ResizeCalculator.Calculate(
                640,
                480,
                settings);

        Assert.AreEqual(1440, result.Width);
        Assert.AreEqual(1080, result.Height);
    }

    [TestMethod]
    public void Calculate_ExactSize_RemainsUnchanged()
    {
        var settings = new ResizeSettings
        {
            Bounds = new ResizeBounds(1920, 1080)
        };

        ResizeDimensions result =
            ResizeCalculator.Calculate(
                1920,
                1080,
                settings);

        Assert.AreEqual(1920, result.Width);
        Assert.AreEqual(1080, result.Height);
    }

    [TestMethod]
    public void Calculate_SquareImage_FitsWithinWideTarget()
    {
        var settings = new ResizeSettings
        {
            Bounds = new ResizeBounds(1920, 1080)
        };

        ResizeDimensions result =
            ResizeCalculator.Calculate(
                2000,
                2000,
                settings);

        Assert.AreEqual(1080, result.Width);
        Assert.AreEqual(1080, result.Height);
    }

    [TestMethod]
    public void Calculate_InvalidOriginalWidth_ThrowsException()
    {
        var settings = new ResizeSettings
        {
            Bounds = new ResizeBounds(1920, 1080)
        };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => ResizeCalculator.Calculate(
                0,
                1080,
                settings));
    }

    [TestMethod]
    public void Calculate_CropMode_ThrowsUntilImplemented()
    {
        var settings = new ResizeSettings
        {
            Bounds = new ResizeBounds(1920, 1080),
            Mode = ImageResizeMode.Crop
        };

        Assert.ThrowsExactly<NotSupportedException>(
            () => ResizeCalculator.Calculate(
                4000,
                3000,
                settings));
    }
}
