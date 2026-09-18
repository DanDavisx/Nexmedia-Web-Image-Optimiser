namespace NexMedia.WebImageOptimiser.Core.Configuration;

public static class PresetCatalog
{
    public static IReadOnlyList<SizePreset> Defaults { get; } = Array.AsReadOnly<SizePreset>(
    [
        new("Banner · 1920 × 600", "Banners", new(1920, 600)),
        new("Banner · 1600 × 400", "Banners", new(1600, 400)),
        new("Content · 1200 px wide", "Content images", new(1200, null)),
        new("Content · 800 px wide", "Content images", new(800, null)),
        new("Content · 400 px wide", "Content images", new(400, null)),
        new("Icon · 16 × 16", "Icons", new(16, 16)),
        new("Icon · 32 × 32", "Icons", new(32, 32)),
        new("Icon · 64 × 64", "Icons", new(64, 64)),
        new("Icon · 128 × 128", "Icons", new(128, 128)),
        new("Logo · 250 × 100", "Logos", new(250, 100)),
        new("Logo · 500 × 200", "Logos", new(500, 200))
    ]);
}
