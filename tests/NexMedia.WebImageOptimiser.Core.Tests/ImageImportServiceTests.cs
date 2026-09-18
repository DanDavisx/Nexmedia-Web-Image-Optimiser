// Verifies batch importing, duplicate detection, invalid-file handling, and recursive folder imports.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexMedia.WebImageOptimiser.Core.Importing;
using SkiaSharp;

namespace NexMedia.WebImageOptimiser.Core.Tests;

[TestClass]
public class ImageImportServiceTests
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
    public void ImportFiles_ImportsSupportedImages()
    {
        string jpeg = CreateRaster(
            "photo.jpg",
            SKEncodedImageFormat.Jpeg);

        string png = CreateRaster(
            "graphic.png",
            SKEncodedImageFormat.Png);

        string webp = CreateRaster(
            "banner.webp",
            SKEncodedImageFormat.Webp);

        string svg = CreateSvg("logo.svg");

        ImageImportResult result =
            ImageImportService.ImportFiles(
                [jpeg, png, webp, svg]);

        Assert.AreEqual(
            4,
            result.ImportedImages.Count);

        Assert.AreEqual(
            0,
            result.DuplicatePaths.Count);

        Assert.AreEqual(
            0,
            result.FailedFiles.Count);
    }

    [TestMethod]
    public void ImportFiles_DuplicatePath_IsNotAddedTwice()
    {
        string image = CreateRaster(
            "photo.jpg",
            SKEncodedImageFormat.Jpeg);

        ImageImportResult result =
            ImageImportService.ImportFiles(
                [image, image]);

        Assert.AreEqual(
            1,
            result.ImportedImages.Count);

        Assert.AreEqual(
            1,
            result.DuplicatePaths.Count);
    }

    [TestMethod]
    public void ImportFiles_ExistingImage_IsReportedAsDuplicate()
    {
        string image = CreateRaster(
            "photo.jpg",
            SKEncodedImageFormat.Jpeg);

        ImageImportResult result =
            ImageImportService.ImportFiles(
                [image],
                [image]);

        Assert.AreEqual(
            0,
            result.ImportedImages.Count);

        Assert.AreEqual(
            1,
            result.DuplicatePaths.Count);
    }

    [TestMethod]
    public void ImportFiles_InvalidImage_IsReportedWithoutStoppingImport()
    {
        string valid = CreateRaster(
            "valid.png",
            SKEncodedImageFormat.Png);

        string invalid = Path.Combine(
            _testDirectory,
            "invalid.jpg");

        File.WriteAllText(
            invalid,
            "This is not an image.");

        ImageImportResult result =
            ImageImportService.ImportFiles(
                [invalid, valid]);

        Assert.AreEqual(
            1,
            result.ImportedImages.Count);

        Assert.AreEqual(
            1,
            result.FailedFiles.Count);
    }

    [TestMethod]
    public void ImportFolder_ImportsSupportedFilesRecursively()
    {
        CreateRaster(
            "root.png",
            SKEncodedImageFormat.Png);

        string nestedDirectory =
            Path.Combine(
                _testDirectory,
                "Nested");

        Directory.CreateDirectory(
            nestedDirectory);

        CreateSvg(
            Path.Combine(
                "Nested",
                "logo.svg"));

        File.WriteAllText(
            Path.Combine(
                _testDirectory,
                "notes.txt"),
            "Ignore me.");

        ImageImportResult result =
            ImageImportService.ImportFolder(
                _testDirectory);

        Assert.AreEqual(
            2,
            result.ImportedImages.Count);

        Assert.AreEqual(
            0,
            result.FailedFiles.Count);
    }

    private string CreateRaster(
        string fileName,
        SKEncodedImageFormat format)
    {
        string filePath =
            Path.Combine(
                _testDirectory,
                fileName);

        string? directory =
            Path.GetDirectoryName(filePath);

        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        using var bitmap =
            new SKBitmap(800, 600);

        using var image =
            SKImage.FromBitmap(bitmap);

        using var data =
            image.Encode(format, 90);

        Assert.IsNotNull(data);

        using var output =
            File.Create(filePath);

        data.SaveTo(output);

        return filePath;
    }

    private string CreateSvg(string fileName)
    {
        string filePath =
            Path.Combine(
                _testDirectory,
                fileName);

        string? directory =
            Path.GetDirectoryName(filePath);

        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(
            filePath,
            """
            <svg xmlns="http://www.w3.org/2000/svg"
                 width="800"
                 height="600"
                 viewBox="0 0 800 600">
            </svg>
            """);

        return filePath;
    }
}
