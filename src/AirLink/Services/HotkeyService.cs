using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace AirLink.Services;

public sealed class HotkeyService : IDisposable
{
    // Win32 modifier flags
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    // Default hotkey: Ctrl+Win+Shift+A
    public const uint DefaultModifiers = MOD_CONTROL | MOD_WIN | MOD_SHIFT;
    public const Keys DefaultKey = Keys.A;

    private const string DefaultRegistryKey = @"SOFTWARE\AirLink";
    private const string ModifiersValueName = "HotkeyModifiers";
    private const string KeyValueName = "HotkeyKey";
    private const string EnabledValueName = "HotkeyEnabled";
    private const int HotkeyId = 1;

    private readonly string _registryKeyPath;
    private readonly HotkeyWindow _window;
    private bool _registered;

    public uint CurrentModifiers { get; private set; }
    public Keys CurrentKey { get; private set; }
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Fires when the registered global hotkey is pressed.
    /// </summary>
    public event Action? HotkeyPressed;

    public HotkeyService() : this(DefaultRegistryKey) { }

    /// <summary>
    /// Parameterized constructor for testability.
    /// </summary>
    public HotkeyService(string registryKeyPath)
    {
        _registryKeyPath = registryKeyPath;
        _window = new HotkeyWindow(OnHotkeyTriggered);
        LoadConfig();

        if (IsEnabled)
            Register(CurrentModifiers, CurrentKey);
    }

    /// <summary>
    /// Loads hotkey configuration from the registry.
    /// Returns false if no config exists (first launch).
    /// </summary>
    public bool LoadConfig()
    {
        return LoadConfig(_registryKeyPath);
    }

    public bool LoadConfig(string registryKeyPath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(registryKeyPath, false);
        if (key is null)
        {
            CurrentModifiers = DefaultModifiers;
            CurrentKey = DefaultKey;
            IsEnabled = false;
            return false;
        }

        var modVal = key.GetValue(ModifiersValueName);
        var keyVal = key.GetValue(KeyValueName);
        var enabledVal = key.GetValue(EnabledValueName);

        if (modVal is null || keyVal is null)
        {
            CurrentModifiers = DefaultModifiers;
            CurrentKey = DefaultKey;
            IsEnabled = false;
            return false;
        }

        CurrentModifiers = (uint)(int)modVal;
        CurrentKey = (Keys)(int)keyVal;
        IsEnabled = enabledVal is int e && e == 1;
        return true;
    }

    /// <summary>
    /// Saves hotkey configuration to the registry.
    /// </summary>
    public void SaveConfig(uint modifiers, Keys key, bool enabled)
    {
        SaveConfig(modifiers, key, enabled, _registryKeyPath);
    }

    public static void SaveConfig(uint modifiers, Keys key, bool enabled, string registryKeyPath)
    {
        using var regKey = Registry.CurrentUser.CreateSubKey(registryKeyPath);
        regKey.SetValue(ModifiersValueName, (int)modifiers, RegistryValueKind.DWord);
        regKey.SetValue(KeyValueName, (int)key, RegistryValueKind.DWord);
        regKey.SetValue(EnabledValueName, enabled ? 1 : 0, RegistryValueKind.DWord);
    }

    /// <summary>
    /// Registers the global hotkey. Unregisters any previous one first.
    /// </summary>
    public bool Register(uint modifiers, Keys key)
    {
        Unregister();

        CurrentModifiers = modifiers;
        CurrentKey = key;

        _registered = NativeMethods.RegisterHotKey(
            _window.Handle, HotkeyId,
            modifiers | MOD_NOREPEAT,
            (uint)key);

        IsEnabled = _registered;
        return _registered;
    }

    /// <summary>
    /// Unregisters the current global hotkey.
    /// </summary>
    public void Unregister()
    {
        if (_registered)
        {
            NativeMethods.UnregisterHotKey(_window.Handle, HotkeyId);
            _registered = false;
        }
    }

    /// <summary>
    /// Applies new hotkey settings: saves to registry and registers/unregisters.
    /// </summary>
    public bool ApplyConfig(uint modifiers, Keys key, bool enabled)
    {
        SaveConfig(modifiers, key, enabled);

        if (enabled)
        {
            return Register(modifiers, key);
        }
        else
        {
            Unregister();
            CurrentModifiers = modifiers;
            CurrentKey = key;
            IsEnabled = false;
            return true;
        }
    }

    /// <summary>
    /// Converts Win32 modifier flags to a human-readable string.
    /// </summary>
    public static string FormatHotkey(uint modifiers, Keys key)
    {
        var parts = new List<string>();
        if ((modifiers & MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((modifiers & MOD_WIN) != 0) parts.Add("Win");
        if ((modifiers & MOD_ALT) != 0) parts.Add("Alt");
        if ((modifiers & MOD_SHIFT) != 0) parts.Add("Shift");
        parts.Add(key.ToString());
        return string.Join("+", parts);
    }

    private void OnHotkeyTriggered()
    {
        HotkeyPressed?.Invoke();
    }

    public void Dispose()
    {
        Unregister();
        _window.DestroyHandle();
    }

    /// <summary>
    /// Hidden window that receives WM_HOTKEY messages.
    /// </summary>
    private sealed class HotkeyWindow : NativeWindow
    {
        private const int WM_HOTKEY = 0x0312;
        private readonly Action _onHotkey;

        public HotkeyWindow(Action onHotkey)
        {
            _onHotkey = onHotkey;
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                _onHotkey();
            }
            base.WndProc(ref m);
        }
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
