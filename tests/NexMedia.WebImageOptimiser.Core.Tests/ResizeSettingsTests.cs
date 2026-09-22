// Verifies the default resizing behaviour and storage of requested resize settings.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexMedia.WebImageOptimiser.Core.Configuration;
using NexMedia.WebImageOptimiser.Core.Processing;

namespace NexMedia.WebImageOptimiser.Core.Tests;

[TestClass]
public class ResizeSettingsTests
{
    [TestMethod]
    public void Defaults_DisableUpscalingAndUseFitMode()
    {
        var settings = new ResizeSettings
        {
            Bounds = new ResizeBounds(1920, 1080)
        };

        Assert.IsFalse(settings.AllowUpscaling);
        Assert.AreEqual(ImageResizeMode.Fit, settings.Mode);
    }

    [TestMethod]
    public void Settings_StoreRequestedDimensions()
    {
        var settings = new ResizeSettings
        {
            Bounds = new ResizeBounds(1200, 800)
        };

        Assert.AreEqual(1200, settings.Bounds.Width);

        Assert.AreEqual(800, settings.Bounds.Height);
    }

    [TestMethod]
    public void Calculate_WidthOnlyPreset_UsesWidthAsMaximum()
    {
        var settings = new ResizeSettings
        {
            Bounds = new ResizeBounds(1200, null)
        };

        ResizeDimensions result =
            ResizeCalculator.Calculate(
                4000,
                3000,
                settings);

        Assert.AreEqual(1200, result.Width);
        Assert.AreEqual(900, result.Height);
    }
}
