namespace NexMedia.WebImageOptimiser.Core.Configuration;

public static class PresetCatalog
{
    public static IReadOnlyList<SizePreset> Defaults { get; } = Array.AsReadOnly<SizePreset>(
    [
        new("Banner · Large · 1920 × 1080", "Banners", new(1920, 1080)),
        new("Banner · Medium · 1920 × 600", "Banners", new(1920, 600)),
        new("Banner · Small · 1600 × 400", "Banners", new(1600, 400)),
        new("Content · Large · 1200 px wide", "Content images", new(1200, null)),
        new("Content · Medium · 800 px wide", "Content images", new(800, null)),
        new("Content · Small · 400 px wide", "Content images", new(400, null)),
        new("Icon · Large · 128 × 128", "Icons", new(128, 128)),
        new("Icon · Medium · 64 × 64", "Icons", new(64, 64)),
        new("Icon · Small · 32 × 32", "Icons", new(32, 32)),
        new("Icon · Tiny · 16 × 16", "Icons", new(16, 16)),
        new("Logo · Large · 500 × 200", "Logos", new(500, 200)),
        new("Logo · Medium · 250 × 100", "Logos", new(250, 100)),
    ]);
}
