using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using NexMedia.WebImageOptimiser.Core.Configuration;
using NexMedia.WebImageOptimiser.Core.Importing;
using NexMedia.WebImageOptimiser.Core.Models;
using NexMedia.WebImageOptimiser.Core.Processing;

namespace NexMedia.WebImageOptimiser.Desktop.ViewModels;

public sealed record ResizeModeOption(
    string Name,
    ResizeMode Mode);

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private AppTheme selectedTheme;
    private bool interfaceAnimationsEnabled = true;

    public IReadOnlyList<AppTheme> Themes { get; } = Enum.GetValues<AppTheme>();

    public AppTheme SelectedTheme
    {
        get => selectedTheme;
        set
        {
            if (selectedTheme == value) return;
            selectedTheme = value;
            PropertyChanged?.Invoke(this, new(nameof(SelectedTheme)));
        }
    }

    public bool InterfaceAnimationsEnabled
    {
        get => interfaceAnimationsEnabled;
        set
        {
            if (interfaceAnimationsEnabled == value) return;
            interfaceAnimationsEnabled = value;
            PropertyChanged?.Invoke(this, new(nameof(InterfaceAnimationsEnabled)));
        }
    }

    public void LoadPreferences(AppPreferences preferences)
    {
        preferences.Validate();
        SelectedTheme = preferences.Theme;
        InterfaceAnimationsEnabled = preferences.InterfaceAnimationsEnabled;
        SelectedPreset = Presets.First(p => p.Name == preferences.PresetName);
        SelectedOutputFormat = preferences.OutputFormat;
        TargetSizeKilobytesText = preferences.TargetSizeKilobytes.ToString(CultureInfo.InvariantCulture);
        MinimumWebPQualityText = preferences.MinimumWebPQuality.ToString(CultureInfo.InvariantCulture);
        SelectedResizeMode = ResizeModes.First(m => m.Mode == preferences.ResizeMode);
        AllowUpscaling = preferences.AllowUpscaling;
        AllowFurtherDimensionReduction = preferences.AllowFurtherDimensionReduction;
    }

    public bool TryCreatePreferences(out AppPreferences preferences, out string validationMessage)
    {
        preferences = new();
        if (!TryCreateOptimisationSettings(out OptimisationSettings? settings, out validationMessage))
            return false;

        preferences = new AppPreferences
        {
            Theme = SelectedTheme,
            InterfaceAnimationsEnabled = InterfaceAnimationsEnabled,
            PresetName = SelectedPreset.Name,
            OutputFormat = SelectedOutputFormat,
            TargetSizeKilobytes = settings!.TargetSizeBytes / 1000,
            MinimumWebPQuality = int.Parse(MinimumWebPQualityText, CultureInfo.InvariantCulture),
            ResizeMode = SelectedResizeMode.Mode,
            AllowUpscaling = AllowUpscaling,
            AllowFurtherDimensionReduction = AllowFurtherDimensionReduction
        };
        return true;
    }

    private static readonly IReadOnlyList<ResizeModeOption> ResizeModeOptions =
    [
        new("Fit within dimensions", ResizeMode.FitWithin),
        new("Crop to fill", ResizeMode.CropToFill)
    ];

    private SizePreset selectedPreset = PresetCatalog.Defaults[2];

    private ResizeModeOption selectedResizeMode = ResizeModeOptions[0];

    private bool allowUpscaling;

    private bool isImporting;

    public ObservableCollection<ImageBatchItem> Images { get; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<SizePreset> Presets => PresetCatalog.Defaults;

    public IReadOnlyList<ResizeModeOption> ResizeModes => ResizeModeOptions;

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

    public ResizeModeOption SelectedResizeMode
    {
        get => selectedResizeMode;

        set
        {
            if (value is null || value == selectedResizeMode)
            {
                return;
            }

            selectedResizeMode = value;

            PropertyChanged?.Invoke(
                this,
                new(nameof(SelectedResizeMode)));
            PropertyChanged?.Invoke(
                this,
                new(nameof(PresetDimensions)));
        }
    }

    public bool AllowUpscaling
    {
        get => allowUpscaling;

        set
        {
            if (value == allowUpscaling)
            {
                return;
            }

            allowUpscaling = value;
            PropertyChanged?.Invoke(
                this,
                new(nameof(AllowUpscaling)));
        }
    }

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

        if (SelectedResizeMode.Mode == ResizeMode.CropToFill &&
            (SelectedPreset.Bounds.Width is null ||
             SelectedPreset.Bounds.Height is null))
        {
            validationMessage =
                "Crop to fill requires a preset with both width and height.";

            return false;
        }

        settings = new OptimisationSettings(
            SelectedPreset.Bounds,
            SelectedOutputFormat,
            SelectedResizeMode.Mode,
            targetKilobytes * 1000,
            minimumQuality,
            AllowFurtherDimensionReduction,
            AllowUpscaling);

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

    public string PresetDimensions =>
        (SelectedResizeMode.Mode, SelectedPreset.Bounds) switch
    {
        (ResizeMode.CropToFill, { Width: int width, Height: int height }) =>
            $"Crop to fill {width} × {height} px",
        (ResizeMode.CropToFill, _) =>
            "Crop to fill requires both width and height",
        (ResizeMode.FitWithin, { Width: int width, Height: int height }) =>
            $"Fit within {width} × {height} px",
        (ResizeMode.FitWithin, { Width: int width }) =>
            $"Maximum width: {width} px",
        (ResizeMode.FitWithin, { Height: int height }) =>
            $"Maximum height: {height} px",
        _ => throw new InvalidOperationException("Preset must specify a dimension.")
    };

    public string DefaultTargetLabel => string.Create(
        CultureInfo.InvariantCulture,
        $"{OptimisationSettings.DefaultTargetSizeBytes / 1000} KB per image");
}
