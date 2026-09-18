// Represents a file that could not be imported and records the reason it was rejected.

namespace NexMedia.WebImageOptimiser.Core.Importing;

public sealed record ImageImportIssue(
    string FilePath,
    string Reason);
