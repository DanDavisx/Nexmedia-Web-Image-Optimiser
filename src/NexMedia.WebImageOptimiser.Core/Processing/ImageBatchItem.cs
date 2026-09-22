// Store each imported image's planned processing settings and output dimensions.

using System.ComponentModel;
using System.IO;
using NexMedia.WebImageOptimiser.Core.Configuration;
using NexMedia.WebImageOptimiser.Core.Models;

namespace NexMedia.WebImageOptimiser.Core.Processing;

public sealed class ImageBatchItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public ImageEntry Source { get; }

    public ResizeSettings? ResizeSettings { get; private set; }

    public ResizeDimensions? PlannedDimensions { get; private set; }

    public string FilePath => Source.FilePath;

    public string FileName => Source.FileName;

    public ImageFileFormat Format => Source.Format;

    public double Width => Source.Width;

    public double Height => Source.Height;

    public long OriginalSizeBytes => Source.OriginalSizeBytes;

    public OptimisationSettings? AssignedOptimisationSettings
    {
        get;
        private set;
    }

    public AutoOptimisationResult? OptimisationResult =>
        optimisationResult;

    public string Status => status;

    public string? ErrorMessage => errorMessage;

    public string FinalDimensionsText =>
        optimisationResult is null
            ? "—"
            : $"{optimisationResult.Width} × {optimisationResult.Height}";

    public string FinalSizeText =>
        optimisationResult is null
            ? "—"
            : $"{optimisationResult.SizeBytes:N0} bytes";

    public string SavingsText =>
        optimisationResult is null || OriginalSizeBytes == 0
            ? "—"
            : $"{(1d - ((double)optimisationResult.SizeBytes / OriginalSizeBytes)) * 100d:0.0}%";

    public string QualityText =>
        optimisationResult?.Quality is int quality
            ? quality.ToString()
            : "—";

    public string TargetResultText =>
        optimisationResult is null
            ? "—"
            : optimisationResult.TargetMet
                ? "Achieved"
                : "Not achieved";

    private AutoOptimisationResult? optimisationResult;

    private string status = "Ready";

    private string? errorMessage;

    private string? exportPath;

    public string PlannedDimensionsText =>
        PlannedDimensions is null
            ? "Dimensions not set"
            : $"{PlannedDimensions.Width} × {PlannedDimensions.Height}";

    public string? ExportPath => exportPath;

    public string ExportFileNameText =>
        exportPath is null
            ? "Not yet exported"
            : Path.GetFileName(exportPath);

    public ImageBatchItem(ImageEntry source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Source = source;
    }

    public void ApplyResizeSettings(ResizeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        ResizeDimensions dimensions = ResizeCalculator.Calculate(Width, Height, settings);

        ResizeSettings = settings;
        PlannedDimensions = dimensions;

        PropertyChanged?.Invoke(this, new(nameof(ResizeSettings)));

        PropertyChanged?.Invoke(this, new(nameof(PlannedDimensions)));

        PropertyChanged?.Invoke(this, new(nameof(PlannedDimensionsText)));
    }

    public void ApplyOptimisationSettings(
        OptimisationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var resizeSettings = new ResizeSettings
        {
            Bounds = settings.Bounds,
            AllowUpscaling = settings.AllowUpscaling,

            Mode = settings.ResizeMode switch
            {
                ResizeMode.FitWithin =>
                    ImageResizeMode.Fit,

                ResizeMode.CropToFill =>
                    ImageResizeMode.Crop,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(settings))
            }
        };

        ApplyResizeSettings(resizeSettings);

        AssignedOptimisationSettings = settings;
        optimisationResult = null;
        status = "Ready";
        errorMessage = null;
        exportPath = null;

        NotifyOptimisationChanged();
    }

    public void MarkProcessing()
    {
        status = "Processing";
        errorMessage = null;
        exportPath = null;

        NotifyOptimisationChanged();
    }

    public void MarkExported(string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        exportPath = outputPath;
        status = "Exported";
        errorMessage = null;

        NotifyOptimisationChanged();
    }

    public void MarkExportFailed(string message)
    {
        status = "Export failed";
        errorMessage = message;

        NotifyOptimisationChanged();
    }

    public void SetOptimisationResult(
        AutoOptimisationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        optimisationResult = result;

        status = result.TargetMet
            ? "Complete"
            : "Target missed";

        errorMessage = null;
        exportPath = null;

        NotifyOptimisationChanged();
    }

    public void MarkFailed(string message)
    {
        optimisationResult = null;
        status = "Failed";
        errorMessage = message;

        NotifyOptimisationChanged();
    }

    private void NotifyOptimisationChanged()
    {
        string[] propertyNames =
        [
            nameof(AssignedOptimisationSettings),
            nameof(OptimisationResult),
            nameof(Status),
            nameof(ErrorMessage),
            nameof(FinalDimensionsText),
            nameof(FinalSizeText),
            nameof(SavingsText),
            nameof(QualityText),
            nameof(TargetResultText),
            nameof(ExportPath),
            nameof(ExportFileNameText)
        ];

        foreach (string propertyName in propertyNames)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
