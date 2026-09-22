using NexMedia.WebImageOptimiser.Core.Configuration;

namespace NexMedia.WebImageOptimiser.Core.Tests;

[TestClass]
public sealed class SettingsValidationTests
{
    [TestMethod]
    [DataRow(null, null)]
    [DataRow(0, 100)]
    [DataRow(-1, 100)]
    [DataRow(100, 0)]
    [DataRow(100, -1)]
    public void RejectsMissingOrNonpositiveDimensions(int? width, int? height)
    {
        Assert.Throws<ArgumentException>(() => new ResizeBounds(width, height));
    }

    [TestMethod]
    [DataRow(1200, null)]
    [DataRow(null, 600)]
    public void UnconstrainedAxisIsValidForFitButRejectedForCrop(int? width, int? height)
    {
        var bounds = new ResizeBounds(width, height);

        var fit = new OptimisationSettings(bounds);

        Assert.AreEqual(ResizeMode.FitWithin, fit.ResizeMode);
        Assert.Throws<ArgumentException>(() => new OptimisationSettings(bounds, resizeMode: ResizeMode.CropToFill));
    }

    [TestMethod]
    [DataRow(0L)]
    [DataRow(-1L)]
    public void RejectsUnusableByteTargets(long bytes)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OptimisationSettings(new(800, 600), targetSizeBytes: bytes));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(101)]
    public void RejectsQualityOutsideAllowedRange(int quality)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OptimisationSettings(new(800, 600), minimumWebPQuality: quality));
    }

    [TestMethod]
    public void RejectsUnknownModesAndFormats()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OptimisationSettings(new(800, 600), outputFormat: (RasterOutputFormat)99));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OptimisationSettings(new(800, 600), resizeMode: (ResizeMode)99));
    }

    [TestMethod]
    public void DefaultsRequireExplicitConsentForCropAndFurtherReduction()
    {
        var settings = new OptimisationSettings(new(1920, 600));

        Assert.AreEqual(ResizeMode.FitWithin, settings.ResizeMode);
        Assert.IsFalse(settings.AllowFurtherDimensionReduction);
        Assert.IsFalse(settings.AllowUpscaling);
        Assert.AreEqual(RasterOutputFormat.WebP, settings.OutputFormat);
        Assert.AreEqual(200_000L, settings.TargetSizeBytes);
    }
}
