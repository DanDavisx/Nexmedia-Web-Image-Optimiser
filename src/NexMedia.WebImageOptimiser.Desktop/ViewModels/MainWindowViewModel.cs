using System.ComponentModel;
using System.Globalization;
using NexMedia.WebImageOptimiser.Core.Configuration;

namespace NexMedia.WebImageOptimiser.Desktop.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private SizePreset selectedPreset = PresetCatalog.Defaults[2];

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
