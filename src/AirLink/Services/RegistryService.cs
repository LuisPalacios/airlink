using Microsoft.Win32;

namespace AirLink.Services;

public static class RegistryService
{
    private const string DefaultRunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string DefaultAppName = "AirLink";
    private const string AppSettingsKey = @"SOFTWARE\AirLink";
    private const string NotificationsValueName = "Notifications";

    public static bool IsStartupEnabled() =>
        IsStartupEnabled(DefaultRunKey, DefaultAppName);

    public static void SetStartupEnabled(bool enabled) =>
        SetStartupEnabled(enabled, DefaultRunKey, DefaultAppName);

    // Parameterized overloads for testability
    public static bool IsStartupEnabled(string runKeyPath, string appName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(runKeyPath, false);
        return key?.GetValue(appName) is not null;
    }

    public static void SetStartupEnabled(bool enabled, string runKeyPath, string appName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(runKeyPath, true)
            ?? Registry.CurrentUser.CreateSubKey(runKeyPath);

        if (enabled)
        {
            var exePath = Environment.ProcessPath
                ?? throw new InvalidOperationException("Cannot determine executable path");
            key.SetValue(appName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(appName, throwOnMissingValue: false);
        }
    }

    public static bool IsNotificationsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(AppSettingsKey, false);
        var value = key?.GetValue(NotificationsValueName);
        // Default to enabled if not set
        return value is not int intVal || intVal == 1;
    }

    public static void SetNotificationsEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(AppSettingsKey);
        key.SetValue(NotificationsValueName, enabled ? 1 : 0, RegistryValueKind.DWord);
    }
}
