// Verifies raster metadata reading for JPEG, PNG, and WebP files, including handling of invalid and missing files.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexMedia.WebImageOptimiser.Core.Imaging;
using NexMedia.WebImageOptimiser.Core.Models;
using SkiaSharp;

namespace NexMedia.WebImageOptimiser.Core.Tests;

[TestClass]
public class RasterMetadataReaderTests
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
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void Read_Jpeg_ReturnsCorrectMetadata()
    {
        string filePath = CreateTestImage(
            "test.jpg",
            SKEncodedImageFormat.Jpeg,
            800,
            600);

        ImageEntry result = RasterMetadataReader.Read(filePath);

        Assert.AreEqual("test.jpg", result.FileName);
        Assert.AreEqual(ImageFileFormat.Jpeg, result.Format);
        Assert.AreEqual(800d, result.Width);
        Assert.AreEqual(600d, result.Height);
        Assert.IsTrue(result.OriginalSizeBytes > 0);
    }

    [TestMethod]
    public void Read_Png_ReturnsCorrectMetadata()
    {
        string filePath = CreateTestImage(
            "test.png",
            SKEncodedImageFormat.Png,
            640,
            480);

        ImageEntry result = RasterMetadataReader.Read(filePath);

        Assert.AreEqual(ImageFileFormat.Png, result.Format);
        Assert.AreEqual(640d, result.Width);
        Assert.AreEqual(480d, result.Height);
        Assert.IsTrue(result.OriginalSizeBytes > 0);
    }

    [TestMethod]
    public void Read_WebP_ReturnsCorrectMetadata()
    {
        string filePath = CreateTestImage(
            "test.webp",
            SKEncodedImageFormat.Webp,
            1920,
            1080);

        ImageEntry result = RasterMetadataReader.Read(filePath);

        Assert.AreEqual(ImageFileFormat.WebP, result.Format);
        Assert.AreEqual(1920d, result.Width);
        Assert.AreEqual(1080d, result.Height);
        Assert.IsTrue(result.OriginalSizeBytes > 0);
    }

    [TestMethod]
    public void Read_InvalidFile_ThrowsInvalidDataException()
    {
        string filePath = Path.Combine(
            _testDirectory,
            "not-an-image.jpg");

        File.WriteAllText(
            filePath,
            "This is not actually an image.");

        Assert.ThrowsExactly<InvalidDataException>(
            () => RasterMetadataReader.Read(filePath));
    }

    [TestMethod]
    public void Read_MissingFile_ThrowsFileNotFoundException()
    {
        string filePath = Path.Combine(
            _testDirectory,
            "missing.jpg");

        Assert.ThrowsExactly<FileNotFoundException>(
            () => RasterMetadataReader.Read(filePath));
    }

    private string CreateTestImage(
        string fileName,
        SKEncodedImageFormat format,
        int width,
        int height)
    {
        string filePath = Path.Combine(
            _testDirectory,
            fileName);

        using var bitmap = new SKBitmap(width, height);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);

        Assert.IsNotNull(data);

        using var output = File.Create(filePath);
        data.SaveTo(output);

        return filePath;
    }
}
