using System.Configuration;

namespace at365.Shell.Properties;

// Keep the original fully qualified type name so LocalFileSettingsProvider can
// read the previous user.config. All new reads/writes use ApplicationSettings.
internal sealed class Settings : ApplicationSettingsBase
{
    [UserScopedSetting, DefaultSettingValue("0")]
    public int Monitor => (int)this[nameof(Monitor)];

    [UserScopedSetting, DefaultSettingValue("0")]
    public int Alignment => (int)this[nameof(Alignment)];

    [UserScopedSetting, DefaultSettingValue("True")]
    public bool Visible => (bool)this[nameof(Visible)];

    [UserScopedSetting, DefaultSettingValue("False")]
    public bool AutoLockEnabled => (bool)this[nameof(AutoLockEnabled)];
}
