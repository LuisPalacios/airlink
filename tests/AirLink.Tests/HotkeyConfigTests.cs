using AirLink.Services;
using Microsoft.Win32;

namespace AirLink.Tests;

public class HotkeyConfigTests : IDisposable
{
    private const string TestRegistryKey = @"SOFTWARE\AirLink\Tests\Hotkey";

    public HotkeyConfigTests()
    {
        Registry.CurrentUser.CreateSubKey(TestRegistryKey)?.Dispose();
    }

    public void Dispose()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(@"SOFTWARE\AirLink\Tests", false);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        HotkeyService.SaveConfig(
            HotkeyService.MOD_CONTROL | HotkeyService.MOD_SHIFT,
            Keys.B,
            true,
            TestRegistryKey);

        using var service = new HotkeyService(TestRegistryKey);

        Assert.Equal(HotkeyService.MOD_CONTROL | HotkeyService.MOD_SHIFT, service.CurrentModifiers);
        Assert.Equal(Keys.B, service.CurrentKey);
        Assert.True(service.IsEnabled);
    }

    [Fact]
    public void LoadConfig_WhenEmpty_ReturnsDefaults()
    {
        // Use a subkey that doesn't exist
        using var service = new HotkeyService(@"SOFTWARE\AirLink\Tests\NonExistent");

        Assert.Equal(HotkeyService.DefaultModifiers, service.CurrentModifiers);
        Assert.Equal(HotkeyService.DefaultKey, service.CurrentKey);
        Assert.False(service.IsEnabled);
    }

    [Fact]
    public void SaveConfig_Disabled_PersistsDisabledState()
    {
        HotkeyService.SaveConfig(
            HotkeyService.MOD_ALT,
            Keys.Z,
            false,
            TestRegistryKey);

        using var service = new HotkeyService(TestRegistryKey);

        Assert.Equal(HotkeyService.MOD_ALT, service.CurrentModifiers);
        Assert.Equal(Keys.Z, service.CurrentKey);
        Assert.False(service.IsEnabled);
    }

    [Theory]
    [InlineData(HotkeyService.MOD_CONTROL, Keys.A, "Ctrl+A")]
    [InlineData(HotkeyService.MOD_CONTROL | HotkeyService.MOD_SHIFT, Keys.F5, "Ctrl+Shift+F5")]
    [InlineData(HotkeyService.MOD_CONTROL | HotkeyService.MOD_WIN | HotkeyService.MOD_SHIFT, Keys.A, "Ctrl+Win+Shift+A")]
    [InlineData(HotkeyService.MOD_ALT, Keys.Space, "Alt+Space")]
    public void FormatHotkey_FormatsCorrectly(uint modifiers, Keys key, string expected)
    {
        Assert.Equal(expected, HotkeyService.FormatHotkey(modifiers, key));
    }
}
