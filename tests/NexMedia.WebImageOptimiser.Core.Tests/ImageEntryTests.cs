// Verifies that ImageEntry correctly stores image metadata and derives the file name from the file path.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexMedia.WebImageOptimiser.Core.Models;

namespace NexMedia.WebImageOptimiser.Core.Tests;

[TestClass]
public class ImageEntryTests
{
    [TestMethod]
    public void FileName_IsDerivedFromFilePath()
    {
        var entry = new ImageEntry
        {
            FilePath = @"C:\Images\hero-image.webp",
            Format = ImageFileFormat.WebP,
            Width = 1920,
            Height = 1080,
            OriginalSizeBytes = 250000
        };

        Assert.AreEqual("hero-image.webp", entry.FileName);
    }

    [TestMethod]
    public void ImageEntry_StoresImageMetadata()
    {
        var entry = new ImageEntry
        {
            FilePath = @"C:\Images\photo.jpg",
            Format = ImageFileFormat.Jpeg,
            Width = 1920,
            Height = 1080,
            OriginalSizeBytes = 500000
        };

        Assert.AreEqual(ImageFileFormat.Jpeg, entry.Format);
        Assert.AreEqual(1920, entry.Width);
        Assert.AreEqual(1080, entry.Height);
        Assert.AreEqual(500000, entry.OriginalSizeBytes);
    }
}
