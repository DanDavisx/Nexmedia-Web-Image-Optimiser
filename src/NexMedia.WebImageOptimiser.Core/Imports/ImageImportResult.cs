// Contains the outcome of a batch import, including imported images, duplicates, and files that could not be read.

using NexMedia.WebImageOptimiser.Core.Models;

namespace NexMedia.WebImageOptimiser.Core.Importing;

public sealed record ImageImportResult(
    IReadOnlyList<ImageEntry> ImportedImages,
    IReadOnlyList<string> DuplicatePaths,
    IReadOnlyList<ImageImportIssue> FailedFiles);
