using System.IO;
using NexMedia.WebImageOptimiser.Core.Configuration;
using NexMedia.WebImageOptimiser.Core.Processing;

namespace NexMedia.WebImageOptimiser.Core.Exporting;

public static class OptimisedImageExportService
{
    public static async Task<string> ExportAsync(
        ImageBatchItem item,
        string destinationFolder,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (string.IsNullOrWhiteSpace(destinationFolder))
        {
            throw new ArgumentException(
                "An export folder is required.",
                nameof(destinationFolder));
        }

        AutoOptimisationResult result =
            item.OptimisationResult ??
            throw new InvalidOperationException(
                "The image has not been optimised successfully.");

        string folder =
            Path.GetFullPath(destinationFolder);

        Directory.CreateDirectory(folder);

        string extension = result.Format switch
        {
            RasterOutputFormat.WebP => ".webp",
            RasterOutputFormat.Png => ".png",

            _ => throw new ArgumentOutOfRangeException(
                nameof(result.Format))
        };

        string sourceName =
            Path.GetFileNameWithoutExtension(item.FileName);

        string temporaryPath =
            Path.Combine(
                folder,
                $".nexmedia-{Guid.NewGuid():N}.tmp");

        try
        {
            await File.WriteAllBytesAsync(
                temporaryPath,
                result.Data,
                cancellationToken);

            for (int number = 1; ; number++)
            {
                string suffix =
                    number == 1
                        ? string.Empty
                        : $"-{number}";

                string outputPath =
                    Path.Combine(
                        folder,
                        $"{sourceName}-optimised{suffix}{extension}");

                if (PathsAreEqual(
                        outputPath,
                        item.FilePath))
                {
                    continue;
                }

                try
                {
                    File.Move(
                        temporaryPath,
                        outputPath);

                    return outputPath;
                }
                catch (IOException)
                    when (File.Exists(outputPath))
                {
                    // Try next numbered filename
                }
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static bool PathsAreEqual(
        string first,
        string second)
    {
        return string.Equals(
            Path.GetFullPath(first),
            Path.GetFullPath(second),
            StringComparison.OrdinalIgnoreCase);
    }
}
