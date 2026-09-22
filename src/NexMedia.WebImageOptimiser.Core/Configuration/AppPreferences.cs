// App preferences

namespace NexMedia.WebImageOptimiser.Core.Configuration;

public enum AppTheme
{
    Dark,
    Light
}

public sealed record AppPreferences
{
    public AppTheme Theme { get; init; } = AppTheme.Dark;
    public bool InterfaceAnimationsEnabled { get; init; } = true;
    public string PresetName { get; init; } = PresetCatalog.Defaults[2].Name;
    public RasterOutputFormat OutputFormat { get; init; } = RasterOutputFormat.WebP;
    public long TargetSizeKilobytes { get; init; } = 200;
    public int MinimumWebPQuality { get; init; } = 60;
    public ResizeMode ResizeMode { get; init; } = ResizeMode.FitWithin;
    public bool AllowUpscaling { get; init; }
    public bool AllowFurtherDimensionReduction { get; init; }

    public void Validate()
    {
        if (!Enum.IsDefined(Theme) || !Enum.IsDefined(OutputFormat) || !Enum.IsDefined(ResizeMode))
            throw new ArgumentException("A saved option is not supported.");

        SizePreset? preset = PresetCatalog.Defaults.FirstOrDefault(p => p.Name == PresetName);
        if (preset is null)
            throw new ArgumentException("The saved size preset is not available.");
        if (TargetSizeKilobytes <= 0 || TargetSizeKilobytes > long.MaxValue / 1000)
            throw new ArgumentException("Maximum file size must be a positive whole number within range.");
        if (MinimumWebPQuality is < 1 or > 100)
            throw new ArgumentException("Minimum WebP quality must be between 1 and 100.");
        if (ResizeMode == ResizeMode.CropToFill &&
            (preset.Bounds.Width is null || preset.Bounds.Height is null))
            throw new ArgumentException("Crop to fill requires both a width and height.");
    }
}
