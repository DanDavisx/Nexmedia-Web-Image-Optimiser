using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexMedia.WebImageOptimiser.Core.Configuration;

public sealed class AppPreferencesStore(string filePath)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public AppPreferences Load(out string? warning)
    {
        warning = null;
        try
        {
            using FileStream stream = File.OpenRead(filePath);
            AppPreferences preferences = JsonSerializer.Deserialize<AppPreferences>(stream, JsonOptions)
                ?? throw new JsonException("Settings were empty.");
            preferences.Validate();
            return preferences;
        }
        catch (FileNotFoundException) { return new(); }
        catch (DirectoryNotFoundException) { return new(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            warning = "Saved settings could not be loaded. Defaults are in use. " + ex.Message;
            return new();
        }
    }

    public void Save(AppPreferences preferences)
    {
        preferences.Validate();
        string fullPath = Path.GetFullPath(filePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        string temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, preferences, JsonOptions);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            try { File.Delete(temporaryPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
