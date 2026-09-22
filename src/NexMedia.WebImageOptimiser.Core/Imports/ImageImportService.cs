// Imports supported image files, routes them to the correct metadata reader, and safely reports duplicates or invalid files.

using NexMedia.WebImageOptimiser.Core.Imaging;
using NexMedia.WebImageOptimiser.Core.Models;

namespace NexMedia.WebImageOptimiser.Core.Importing;

public static class ImageImportService
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

    public static ImageImportResult ImportFiles(
        IEnumerable<string> filePaths,
        IEnumerable<string>? existingFilePaths = null)
    {
        ArgumentNullException.ThrowIfNull(filePaths);

        var importedImages = new List<ImageEntry>();
        var duplicatePaths = new List<string>();
        var failedFiles = new List<ImageImportIssue>();

        var knownPaths = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        if (existingFilePaths is not null)
        {
            foreach (string existingPath in existingFilePaths)
            {
                if (!string.IsNullOrWhiteSpace(existingPath))
                {
                    knownPaths.Add(
                        Path.GetFullPath(existingPath));
                }
            }
        }

        foreach (string filePath in filePaths)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                failedFiles.Add(
                    new ImageImportIssue(
                        filePath ?? string.Empty,
                        "The file path is empty."));

                continue;
            }

            string fullPath;

            try
            {
                fullPath = Path.GetFullPath(filePath);
            }
            catch (Exception exception) when (
                exception is ArgumentException or
                NotSupportedException)
            {
                failedFiles.Add(
                    new ImageImportIssue(
                        filePath,
                        "The file path is invalid."));

                continue;
            }

            if (!IsSupportedFile(fullPath))
            {
                failedFiles.Add(
                    new ImageImportIssue(
                        fullPath,
                        "The file type is not supported."));

                continue;
            }

            if (!knownPaths.Add(fullPath))
            {
                duplicatePaths.Add(fullPath);
                continue;
            }

            try
            {
                ImageEntry entry =
                    RasterMetadataReader.Read(fullPath);

                importedImages.Add(entry);
            }
            catch (Exception exception) when (
                exception is InvalidDataException or
                IOException or
                UnauthorizedAccessException or
                ArgumentException or
                NotSupportedException)
            {
                failedFiles.Add(
                    new ImageImportIssue(
                        fullPath,
                        exception.Message));
            }
        }

        return new ImageImportResult(
            importedImages,
            duplicatePaths,
            failedFiles);
    }

    public static ImageImportResult ImportFolder(
        string folderPath,
        IEnumerable<string>? existingFilePaths = null)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException(
                "A folder path must be provided.",
                nameof(folderPath));
        }

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException(
                $"The folder could not be found: {folderPath}");
        }

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        IEnumerable<string> files =
            Directory
                .EnumerateFiles(folderPath, "*", options)
                .Where(IsSupportedFile);

        return ImportFiles(
            files,
            existingFilePaths);
    }

    private static bool IsSupportedFile(string filePath)
    {
        string extension = Path.GetExtension(filePath);

        return SupportedExtensions.Contains(extension);
    }
}
