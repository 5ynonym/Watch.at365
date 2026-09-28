using System.Configuration;
using System.IO;
using System.Text.Json;

namespace at365.Common365;

internal static class LegacySettingsImporter
{
    // Only used when config.json does not exist. Never writes the legacy files.
    public static ApplicationConfiguration Load()
    {
        var result = new ApplicationConfiguration();
        try
        {
            var legacy = new Shell.Properties.Settings();
            result.Monitor = legacy.Monitor;
            result.Alignment = legacy.Alignment;
            result.Visible = legacy.Visible;
            result.AutoLockEnabled = legacy.AutoLockEnabled;
        }
        catch (Exception error) when (error is ConfigurationErrorsException or IOException or UnauthorizedAccessException)
        { Diagnostics.Report("Import legacy user settings", error); }

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "blacklist.json");
            if (File.Exists(path)) result.Blacklist = JsonSerializer.Deserialize<string[]>(File.ReadAllText(path)) ?? [];
        }
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException)
        { Diagnostics.Report("Import legacy blacklist", error); }
        return result;
    }
}
