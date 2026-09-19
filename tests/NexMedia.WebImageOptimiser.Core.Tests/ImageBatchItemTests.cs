// Verifies that batch items keep original metadata and correctly store calculated resize plans.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexMedia.WebImageOptimiser.Core.Configuration;
using NexMedia.WebImageOptimiser.Core.Models;
using NexMedia.WebImageOptimiser.Core.Processing;

namespace NexMedia.WebImageOptimiser.Core.Tests;

[TestClass]
public class ImageBatchItemTests
{
    [TestMethod]
    public void Constructor_ExposesSourceMetadata()
    {
        var source = new ImageEntry
        {
            FilePath = @"C:\Images\photo.jpg",
            Format = ImageFileFormat.Jpeg,
            Width = 4000,
            Height = 3000,
            OriginalSizeBytes = 500000
        };

        var item = new ImageBatchItem(source);

        Assert.AreEqual(
            "photo.jpg",
            item.FileName);

        Assert.AreEqual(
            ImageFileFormat.Jpeg,
            item.Format);

        Assert.AreEqual(
            4000d,
            item.Width);

        Assert.IsNull(
            item.ResizeSettings);

        Assert.IsNull(
            item.PlannedDimensions);
    }

    [TestMethod]
    public void ApplyResizeSettings_CalculatesPlannedDimensions()
    {
        var source = new ImageEntry
        {
            FilePath = @"C:\Images\photo.jpg",
            Format = ImageFileFormat.Jpeg,
            Width = 4000,
            Height = 3000,
            OriginalSizeBytes = 500000
        };

        var item = new ImageBatchItem(source);

        var settings = new ResizeSettings
        {
            Bounds = new ResizeBounds(1920, 1080)
        };

        item.ApplyResizeSettings(settings);

        Assert.AreSame(
            settings,
            item.ResizeSettings);

        Assert.IsNotNull(
            item.PlannedDimensions);

        Assert.AreEqual(
            1440,
            item.PlannedDimensions.Width);

        Assert.AreEqual(
            1080,
            item.PlannedDimensions.Height);
    }
}
