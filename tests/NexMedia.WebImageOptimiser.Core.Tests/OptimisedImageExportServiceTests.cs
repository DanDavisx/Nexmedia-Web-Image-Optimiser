using NexMedia.WebImageOptimiser.Core.Configuration;
using NexMedia.WebImageOptimiser.Core.Exporting;
using NexMedia.WebImageOptimiser.Core.Models;
using NexMedia.WebImageOptimiser.Core.Processing;

namespace NexMedia.WebImageOptimiser.Core.Tests;

[TestClass]
public sealed class OptimisedImageExportServiceTests
{
    [TestMethod]
    [DataRow(RasterOutputFormat.WebP, ".webp")]
    [DataRow(RasterOutputFormat.Png, ".png")]
    public async Task ExportAsync_UsesOutputFormatExtensionAndExactBytes(
        RasterOutputFormat format,
        string expectedExtension)
    {
        string directory = CreateTestDirectory();

        try
        {
            byte[] expectedData = [1, 3, 5, 7, 9];

            ImageBatchItem item =
                CreateCompletedItem(
                    Path.Combine(directory, "photo.jpg"),
                    format,
                    expectedData);

            string outputPath =
                await OptimisedImageExportService.ExportAsync(
                    item,
                    directory);

            Assert.AreEqual(
                expectedExtension,
                Path.GetExtension(outputPath));

            CollectionAssert.AreEqual(
                expectedData,
                await File.ReadAllBytesAsync(outputPath));
        }
        finally
        {
            DeleteTestDirectory(directory);
        }
    }

    [TestMethod]
    public async Task ExportAsync_UsesNumberedNameWithoutOverwritingExistingFile()
    {
        string directory = CreateTestDirectory();

        try
        {
            string existingPath =
                Path.Combine(
                    directory,
                    "photo-optimised.webp");

            byte[] existingData = [20, 21];

            await File.WriteAllBytesAsync(
                existingPath,
                existingData);

            ImageBatchItem item =
                CreateCompletedItem(
                    Path.Combine(directory, "photo.jpg"),
                    RasterOutputFormat.WebP,
                    [1, 2, 3]);

            string outputPath =
                await OptimisedImageExportService.ExportAsync(
                    item,
                    directory);

            Assert.AreEqual(
                "photo-optimised-2.webp",
                Path.GetFileName(outputPath));

            CollectionAssert.AreEqual(
                existingData,
                await File.ReadAllBytesAsync(existingPath));
        }
        finally
        {
            DeleteTestDirectory(directory);
        }
    }

    [TestMethod]
    public async Task ExportAsync_DoesNotModifyOriginalInDestinationFolder()
    {
        string directory = CreateTestDirectory();

        try
        {
            string sourcePath =
                Path.Combine(directory, "photo.webp");

            byte[] originalData = [40, 41, 42];

            await File.WriteAllBytesAsync(
                sourcePath,
                originalData);

            ImageBatchItem item =
                CreateCompletedItem(
                    sourcePath,
                    RasterOutputFormat.WebP,
                    [1, 2, 3]);

            string outputPath =
                await OptimisedImageExportService.ExportAsync(
                    item,
                    directory);

            Assert.AreNotEqual(
                Path.GetFullPath(sourcePath),
                Path.GetFullPath(outputPath));

            CollectionAssert.AreEqual(
                originalData,
                await File.ReadAllBytesAsync(sourcePath));
        }
        finally
        {
            DeleteTestDirectory(directory);
        }
    }

    [TestMethod]
    public async Task ExportAsync_WithoutOptimisationResult_Throws()
    {
        string directory = CreateTestDirectory();

        try
        {
            var item = new ImageBatchItem(
                CreateSource(
                    Path.Combine(directory, "photo.jpg")));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => OptimisedImageExportService.ExportAsync(
                    item,
                    directory));
        }
        finally
        {
            DeleteTestDirectory(directory);
        }
    }

    [TestMethod]
    public async Task ExportAsync_RemovesTemporaryFileAfterSuccess()
    {
        string directory = CreateTestDirectory();

        try
        {
            ImageBatchItem item =
                CreateCompletedItem(
                    Path.Combine(directory, "photo.jpg"),
                    RasterOutputFormat.Png,
                    [1, 2, 3]);

            await OptimisedImageExportService.ExportAsync(
                item,
                directory);

            string[] temporaryFiles =
                Directory.GetFiles(
                    directory,
                    ".nexmedia-*.tmp");

            Assert.AreEqual(
                0,
                temporaryFiles.Length);
        }
        finally
        {
            DeleteTestDirectory(directory);
        }
    }

    private static ImageBatchItem CreateCompletedItem(
        string sourcePath,
        RasterOutputFormat format,
        byte[] data)
    {
        var item = new ImageBatchItem(
            CreateSource(sourcePath));

        item.SetOptimisationResult(
            new AutoOptimisationResult(
                data,
                format,
                100,
                80,
                data.LongLength,
                format == RasterOutputFormat.WebP
                    ? 80
                    : null,
                200_000,
                true,
                false));

        return item;
    }

    private static ImageEntry CreateSource(
        string sourcePath)
    {
        return new ImageEntry
        {
            FilePath = sourcePath,
            Format = ImageFileFormat.Jpeg,
            Width = 100,
            Height = 80,
            OriginalSizeBytes = 1_000
        };
    }

    private static string CreateTestDirectory()
    {
        string directory =
            Path.Combine(
                Path.GetTempPath(),
                $"NexMediaExportTests-{Guid.NewGuid():N}");

        Directory.CreateDirectory(directory);

        return directory;
    }

    private static void DeleteTestDirectory(
        string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(
                directory,
                true);
        }
    }
}
