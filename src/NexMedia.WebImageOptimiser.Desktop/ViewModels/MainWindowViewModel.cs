using System.ComponentModel;
using System.Globalization;
using System.Collections.ObjectModel;
using NexMedia.WebImageOptimiser.Core.Importing;
using NexMedia.WebImageOptimiser.Core.Models;
using NexMedia.WebImageOptimiser.Core.Configuration;

namespace NexMedia.WebImageOptimiser.Desktop.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private SizePreset selectedPreset = PresetCatalog.Defaults[2];

    private bool isImporting;

    public ObservableCollection<ImageEntry> Images { get; } = [];

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
                Images.Add(image);
            }

            PropertyChanged?.Invoke(
                this,
                new(nameof(ImageCountText)));

            PropertyChanged?.Invoke(
                this,
                new(nameof(IsBatchEmpty)));

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
