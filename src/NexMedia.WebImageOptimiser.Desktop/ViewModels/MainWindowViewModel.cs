using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using NexMedia.WebImageOptimiser.Core.Configuration;
using NexMedia.WebImageOptimiser.Core.Importing;
using NexMedia.WebImageOptimiser.Core.Models;
using NexMedia.WebImageOptimiser.Core.Processing;

namespace NexMedia.WebImageOptimiser.Desktop.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private SizePreset selectedPreset = PresetCatalog.Defaults[2];

    private bool isImporting;

    public ObservableCollection<ImageBatchItem> Images { get; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<SizePreset> Presets => PresetCatalog.Defaults;

    public SizePreset SelectedPreset
    {
        get => selectedPreset;
        set
        {
            if (value is null || value == selectedPreset)
            {
                return;
            }

            selectedPreset = value;
            PropertyChanged?.Invoke(this, new(nameof(SelectedPreset)));
            PropertyChanged?.Invoke(this, new(nameof(PresetDimensions)));
        }
    }

    public void RemoveImages(IEnumerable<ImageBatchItem> images)
    {
        ImageBatchItem[] imagesToRemove = images.ToArray();

        foreach (ImageBatchItem image in imagesToRemove)
        {
            Images.Remove(image);
        }

        NotifyBatchChanged();
    }

    public void ClearImages()
    {
        Images.Clear();

        NotifyBatchChanged();
    }

    private void NotifyBatchChanged()
    {
        PropertyChanged?.Invoke(this, new(nameof(ImageCountText)));
        PropertyChanged?.Invoke(this, new(nameof(IsBatchEmpty)));
    }

    private RasterOutputFormat selectedOutputFormat = RasterOutputFormat.WebP;

    private string targetSizeKilobytesText = "200";

    private string minimumWebPQualityText = "60";

    private bool allowFurtherDimensionReduction;

    public bool TryCreateOptimisationSettings(
        out OptimisationSettings? settings,
        out string validationMessage)
    {
        settings = null;
        validationMessage = string.Empty;

        if (!long.TryParse(
                TargetSizeKilobytesText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long targetKilobytes) ||
            targetKilobytes <= 0)
        {
            validationMessage =
                "Target size must be a positive whole number.";

            return false;
        }

        if (targetKilobytes > long.MaxValue / 1000)
        {
            validationMessage = "Target size is too large.";
            return false;
        }

        if (!int.TryParse(
                MinimumWebPQualityText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int minimumQuality) ||
            minimumQuality is < 1 or > 100)
        {
            validationMessage =
                "Minimum WebP quality must be between 1 and 100.";

            return false;
        }

        settings = new OptimisationSettings(
            SelectedPreset.Bounds,
            SelectedOutputFormat,
            ResizeMode.FitWithin,
            targetKilobytes * 1000,
            minimumQuality,
            AllowFurtherDimensionReduction);

        return true;
    }

    public int ApplyOptimisationSettings(
        IEnumerable<ImageBatchItem> images,
        OptimisationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(images);
        ArgumentNullException.ThrowIfNull(settings);

        int appliedCount = 0;

        foreach (ImageBatchItem image in images)
        {
            if (image.Format == ImageFileFormat.Svg)
            {
                continue;
            }

            image.ApplyOptimisationSettings(settings);
            appliedCount++;
        }

        return appliedCount;
    }

    public bool IsImporting
    {
        get => isImporting;

        private set
        {
            if (value == isImporting)
            {
                return;
            }

            isImporting = value;

            PropertyChanged?.Invoke(
                this,
                new(nameof(IsImporting)));

            PropertyChanged?.Invoke(
                this,
                new(nameof(CanImport)));
        }
    }

    public IReadOnlyList<RasterOutputFormat> OutputFormats { get; } =
        Enum.GetValues<RasterOutputFormat>();

    public RasterOutputFormat SelectedOutputFormat
    {
        get => selectedOutputFormat;

        set
        {
            if (value == selectedOutputFormat)
            {
                return;
            }

            selectedOutputFormat = value;

            PropertyChanged?.Invoke(
                this,
                new(nameof(SelectedOutputFormat)));
        }
    }

    public string TargetSizeKilobytesText
    {
        get => targetSizeKilobytesText;

        set
        {
            targetSizeKilobytesText = value;

            PropertyChanged?.Invoke(
                this,
                new(nameof(TargetSizeKilobytesText)));
        }
    }

    public string MinimumWebPQualityText
    {
        get => minimumWebPQualityText;

        set
        {
            minimumWebPQualityText = value;

            PropertyChanged?.Invoke(
                this,
                new(nameof(MinimumWebPQualityText)));
        }
    }

    public bool AllowFurtherDimensionReduction
    {
        get => allowFurtherDimensionReduction;

        set
        {
            allowFurtherDimensionReduction = value;

            PropertyChanged?.Invoke(
                this,
                new(nameof(AllowFurtherDimensionReduction)));
        }
    }

    public bool CanImport => !IsImporting;

    public bool IsBatchEmpty =>
        Images.Count == 0;

    public string ImageCountText =>
        Images.Count == 1
            ? "1 image"
            : $"{Images.Count} images";

    public Task<ImageImportResult> ImportFilesAsync(
        IEnumerable<string> filePaths)
    {
        return ImportAsync(
            () => ImageImportService.ImportFiles(
                filePaths,
                Images.Select(image => image.FilePath)));
    }

    public Task<ImageImportResult> ImportFolderAsync(
        string folderPath)
    {
        return ImportAsync(
            () => ImageImportService.ImportFolder(
                folderPath,
                Images.Select(image => image.FilePath)));
    }

    private async Task<ImageImportResult> ImportAsync(
        Func<ImageImportResult> importOperation)
    {
        IsImporting = true;

        try
        {
            ImageImportResult result =
                await Task.Run(importOperation);

            foreach (ImageEntry image in result.ImportedImages)
            {
                Images.Add(
                    new ImageBatchItem(image));
            }

            NotifyBatchChanged();

            return result;
        }
        finally
        {
            IsImporting = false;
        }
    }

    public string PresetDimensions => SelectedPreset.Bounds switch
    {
        { Width: int width, Height: int height } => $"Fit within {width} × {height} px",
        { Width: int width } => $"Maximum width: {width} px",
        { Height: int height } => $"Maximum height: {height} px",
        _ => throw new InvalidOperationException("Preset must specify a dimension.")
    };

    public string DefaultTargetLabel => string.Create(
        CultureInfo.InvariantCulture,
        $"{OptimisationSettings.DefaultTargetSizeBytes / 1000} KB per image");
}
