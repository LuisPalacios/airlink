using Microsoft.Win32;

namespace AirLink.Services;

public sealed class ThemeService : IDisposable
{
    private const string ThemeRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string ThemeValueName = "SystemUsesLightTheme";

    public bool IsLightTheme { get; private set; }
    public event Action? ThemeChanged;

    public ThemeService()
    {
        IsLightTheme = ReadThemeFromRegistry();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category != UserPreferenceCategory.General) return;

        var newValue = ReadThemeFromRegistry();
        if (newValue != IsLightTheme)
        {
            IsLightTheme = newValue;
            ThemeChanged?.Invoke();
        }
    }

    /// <summary>
    /// Reads the current taskbar theme from the Windows Registry.
    /// Returns true if light theme, false if dark theme.
    /// </summary>
    public static bool ReadThemeFromRegistry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(ThemeRegistryKey);
        var value = key?.GetValue(ThemeValueName);
        return value is int intVal && intVal == 1;
    }

    public void Dispose()
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
    }
}
