// Define new resolution presets here.

namespace NexMedia.WebImageOptimiser.Core.Configuration;

public static class PresetCatalog
{
    public static IReadOnlyList<SizePreset> Defaults { get; } = Array.AsReadOnly<SizePreset>(
    [
        new("Banner 2048 × 1536", "Banners", new(2048, 1536)),
        new("Banner 1920 × 1080", "Banners", new(1920, 1080)),
        new("Content 1200 x 800", "Content images", new(1200, 1080)),
        new("Content 1080 x 1080", "Content images", new(1080, 1080)),
        new("Content 1080 x 720", "Content images", new(1080, 720)),
        new("Icon 128 × 128", "Icons", new(128, 128)),
        new("Icon 64 × 64", "Icons", new(64, 64)),
        new("Icon 32 × 32", "Icons", new(32, 32)),
        new("Icon 16 × 16", "Icons", new(16, 16)),
        new("Logo 500 × 200", "Logos", new(500, 200)),
        new("Logo 250 × 100", "Logos", new(250, 100)),
    ]);
}
