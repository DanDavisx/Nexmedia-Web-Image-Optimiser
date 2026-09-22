using NexMedia.WebImageOptimiser.Core.Configuration;

namespace NexMedia.WebImageOptimiser.Core.Tests;

[TestClass]
public sealed class AppPreferencesStoreTests
{
    private string directory = null!;
    private string filePath = null!;

    [TestInitialize]
    public void Initialise()
    {
        directory = Path.Combine(Path.GetTempPath(), "NexMediaPreferencesTests", Guid.NewGuid().ToString("N"));
        filePath = Path.Combine(directory, "settings.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    [TestMethod]
    public void MissingFileUsesDefaultsWithoutWarning()
    {
        Assert.AreEqual(new AppPreferences(), new AppPreferencesStore(filePath).Load(out string? warning));
        Assert.IsNull(warning);
        Assert.IsFalse(Directory.Exists(directory));
    }

    [TestMethod]
    public void SaveAndNewStoreRestoreEveryPreference()
    {
        var preferences = new AppPreferences
        {
            Theme = AppTheme.Light,
            InterfaceAnimationsEnabled = false,
            PresetName = PresetCatalog.Defaults[0].Name,
            OutputFormat = RasterOutputFormat.Png,
            TargetSizeKilobytes = 350,
            MinimumWebPQuality = 77,
            ResizeMode = ResizeMode.CropToFill,
            AllowUpscaling = true,
            AllowFurtherDimensionReduction = true
        };
        new AppPreferencesStore(filePath).Save(preferences);
        Assert.AreEqual(preferences, new AppPreferencesStore(filePath).Load(out string? warning));
        Assert.IsNull(warning);
        Assert.HasCount(1, Directory.GetFiles(directory));
    }

    [TestMethod]
    [DataRow("{broken")]
    [DataRow("null")]
    [DataRow("{\"Theme\":\"Unsupported\"}")]
    [DataRow("{\"Theme\":42}")]
    [DataRow("{\"OutputFormat\":42}")]
    [DataRow("{\"ResizeMode\":42}")]
    [DataRow("{\"PresetName\":\"Missing preset\"}")]
    [DataRow("{\"TargetSizeKilobytes\":0}")]
    [DataRow("{\"TargetSizeKilobytes\":9223372036854775807}")]
    [DataRow("{\"MinimumWebPQuality\":101}")]
    [DataRow("{\"MinimumWebPQuality\":0}")]
    [DataRow("{\"PresetName\":\"Content 800 px wide\",\"ResizeMode\":\"CropToFill\"}")]
    public void InvalidFileFallsBackWithoutOverwritingOriginal(string json)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(filePath, json);
        Assert.AreEqual(new AppPreferences(), new AppPreferencesStore(filePath).Load(out string? warning));
        Assert.IsNotNull(warning);
        Assert.AreEqual(json, File.ReadAllText(filePath));
    }

    [TestMethod]
    public void PartialFileUsesDefaultsForNewSettings()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(filePath, "{\"Theme\":\"Light\"}");
        Assert.AreEqual(new AppPreferences { Theme = AppTheme.Light },
            new AppPreferencesStore(filePath).Load(out string? warning));
        Assert.IsNull(warning);
    }

    [TestMethod]
    public void InvalidSavePreservesPreviousPreferences()
    {
        var store = new AppPreferencesStore(filePath);
        var previous = new AppPreferences { Theme = AppTheme.Light };
        store.Save(previous);
        Assert.Throws<ArgumentException>(() => store.Save(previous with { MinimumWebPQuality = 0 }));
        Assert.AreEqual(previous, store.Load(out _));
        Assert.HasCount(1, Directory.GetFiles(directory));
    }

    [TestMethod]
    public void SavingDefaultsReplacesExistingPreferences()
    {
        var store = new AppPreferencesStore(filePath);
        store.Save(new AppPreferences { Theme = AppTheme.Light, InterfaceAnimationsEnabled = false });
        store.Save(new());
        Assert.AreEqual(new AppPreferences(), new AppPreferencesStore(filePath).Load(out string? warning));
        Assert.IsNull(warning);
        Assert.HasCount(1, Directory.GetFiles(directory));
    }

    [TestMethod]
    public void FailedReplacementPreservesDestinationAndCleansTemporaryFile()
    {
        Directory.CreateDirectory(filePath);
        Exception error = Assert.Throws<Exception>(() => new AppPreferencesStore(filePath).Save(new()));
        Assert.IsTrue(error is IOException or UnauthorizedAccessException);
        Assert.IsTrue(Directory.Exists(filePath));
        Assert.IsEmpty(Directory.GetFiles(directory));
    }
}
