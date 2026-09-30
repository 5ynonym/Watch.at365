using System.IO;
using System.Text.Json;

namespace at365.Common365;

internal sealed class ApplicationConfiguration
{
    public int Monitor { get; set; }
    public int Alignment { get; set; }
    public bool Visible { get; set; } = true;
    public bool AutoLockEnabled { get; set; }
    public int ClipboardHistoryLimit { get; set; } = 50;
    public int ClipboardHistoryWidth { get; set; } = 520;
    public int ClipboardHistoryHeight { get; set; } = 640;
    public string[] Blacklist { get; set; } = [];

    internal void Normalize()
    {
        Monitor = Math.Max(0, Monitor);
        ClipboardHistoryLimit = Math.Clamp(ClipboardHistoryLimit, 0, 1000);
        ClipboardHistoryWidth = Math.Clamp(ClipboardHistoryWidth, 280, 1600);
        ClipboardHistoryHeight = Math.Clamp(ClipboardHistoryHeight, 200, 1600);
        Alignment = Alignment == 2 ? 2 : 0; // WPF Bottom / Top
        Blacklist = (Blacklist ?? []).Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim().ToLowerInvariant()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}

internal static class ApplicationSettings
{
    internal static string DirectoryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "at365", "Watch");

    private static readonly ConfigurationStore Store = new(Path.Combine(DirectoryPath, "config.json"));
    // Loading is explicit: constructing UI or running tests must not write user settings.
    public static ApplicationConfiguration Current { get; private set; } = new();
    public static void Initialize() => Current = Store.Load(LegacySettingsImporter.Load);
    public static void Save() => Store.Save(Current);
}

internal sealed class ConfigurationStore(string filePath, Action<string, Exception>? report = null)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
    private readonly Action<string, Exception> _report = report ?? Diagnostics.Report;

    public ApplicationConfiguration Load(Func<ApplicationConfiguration>? importLegacy = null)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                var initial = importLegacy?.Invoke() ?? new ApplicationConfiguration();
                initial.Normalize();
                Save(initial);
                return initial;
            }
            var settings = JsonSerializer.Deserialize<ApplicationConfiguration>(File.ReadAllText(filePath), Options)
                ?? throw new JsonException("Configuration must be an object.");
            settings.Normalize();
            return settings;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            _report("Load config.json", error);
            if (error is JsonException)
            {
                try { File.Copy(filePath, filePath + $".invalid-{Guid.NewGuid():N}"); }
                catch (Exception backupError) when (backupError is IOException or UnauthorizedAccessException)
                { _report("Backup invalid config.json", backupError); }
            }
            return new ApplicationConfiguration();
        }
    }

    public bool Save(ApplicationConfiguration settings)
    {
        var temporaryPath = filePath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            settings.Normalize();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, settings, Options);
                stream.Flush(flushToDisk: true);
            }
            // Same-directory replacement leaves the previous file intact until writing succeeds.
            File.Move(temporaryPath, filePath, overwrite: true);
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            _report("Save config.json", error);
            return false;
        }
        finally
        {
            try { File.Delete(temporaryPath); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            { _report("Clean temporary config", error); }
        }
    }
}
