using System.Reflection;
using AirLink.Services;

namespace AirLink;

public sealed class TrayApplicationContext : ApplicationContext
{
    private const string GitHubUrl = "https://github.com/LuisPalacios/airlink";

    private readonly NotifyIcon _trayIcon;
    private readonly ThemeService _themeService;
    private readonly IconService _iconService;
    private readonly BluetoothService _bluetoothService;
    private readonly HotkeyService _hotkeyService;

    private readonly ToolStripMenuItem _startupItem;
    private readonly ToolStripMenuItem _shortcutItem;
    private readonly ToolStripMenuItem _selectDeviceItem;
    private bool _isConnected;

    public TrayApplicationContext()
    {
        _themeService = new ThemeService();
        _iconService = new IconService();
        _bluetoothService = new BluetoothService();
        _hotkeyService = new HotkeyService();

        // Build context menu
        var helpItem = new ToolStripMenuItem("Help");
        helpItem.Click += OnHelpClicked;

        _selectDeviceItem = new ToolStripMenuItem("Select Device");
        _selectDeviceItem.Click += OnSelectDeviceClicked;
        UpdateSelectDeviceMenuText();

        _shortcutItem = new ToolStripMenuItem("Shortcut");
        _shortcutItem.Click += OnShortcutClicked;
        UpdateShortcutMenuText();

        _startupItem = new ToolStripMenuItem("Run at Startup")
        {
            Checked = RegistryService.IsStartupEnabled(),
            CheckOnClick = true,
        };
        _startupItem.CheckedChanged += (_, _) =>
            RegistryService.SetStartupEnabled(_startupItem.Checked);

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += OnExitClicked;

        var menu = new ContextMenuStrip();
        menu.Items.Add(helpItem);
        menu.Items.Add(_selectDeviceItem);
        menu.Items.Add(_shortcutItem);
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _trayIcon = new NotifyIcon
        {
            Visible = true,
            Text = "AirLink - Disconnected",
            ContextMenuStrip = menu,
        };

        // Left-click: toggle connection or show device picker
        _trayIcon.MouseClick += OnTrayIconClick;

        // Set initial icon based on current theme
        UpdateIcon();

        // Wire theme and connection events
        _themeService.ThemeChanged += UpdateIcon;
        _bluetoothService.ConnectionChanged += OnConnectionChanged;

        // Wire global hotkey
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;

        // Auto-register for startup on first launch
        if (!RegistryService.IsStartupEnabled())
        {
            RegistryService.SetStartupEnabled(true);
            _startupItem.Checked = true;
        }

        // Check if selected device is already connected (e.g., app restarted)
        _ = InitializeConnectionStatusAsync();
    }

    private async Task InitializeConnectionStatusAsync()
    {
        await _bluetoothService.CheckConnectionStatusAsync();
    }

    private async void OnTrayIconClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        await ToggleConnectionAsync();
    }

    private async void OnHotkeyPressed()
    {
        await ToggleConnectionAsync();
    }

    private async Task ToggleConnectionAsync()
    {
        try
        {
            // If no device configured, show picker first
            if (!_bluetoothService.HasSelectedDevice)
            {
                ShowDevicePicker();
                if (!_bluetoothService.HasSelectedDevice)
                    return;
            }

            // Toggle connection
            if (_isConnected)
            {
                await _bluetoothService.DisconnectAsync();
                _trayIcon.ShowBalloonTip(
                    2000, "AirLink",
                    $"{_bluetoothService.SelectedDeviceName ?? "Device"} disconnected.",
                    ToolTipIcon.Info);
                return;
            }

            var success = await _bluetoothService.ConnectAsync();
            var deviceName = _bluetoothService.SelectedDeviceName ?? "Device";
            if (success)
            {
                _trayIcon.ShowBalloonTip(
                    2000, "AirLink",
                    $"Connected to {deviceName}.",
                    ToolTipIcon.Info);
            }
            else
            {
                _trayIcon.ShowBalloonTip(
                    3000, "AirLink",
                    $"Could not connect to {deviceName}. Make sure it is nearby and available.",
                    ToolTipIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _trayIcon.ShowBalloonTip(
                3000, "AirLink",
                $"Connection error: {ex.Message}",
                ToolTipIcon.Error);
        }
    }

    /// <summary>
    /// Shows the device picker. Connects/disconnects based on selection changes.
    /// </summary>
    private async void ShowDevicePicker()
    {
        var previousDeviceId = _bluetoothService.SelectedDeviceId;

        using var form = new DevicePickerForm(
            _bluetoothService.SelectedDeviceName,
            _bluetoothService.SelectedDeviceId);

        if (form.ShowDialog() != DialogResult.OK)
            return;

        var newDeviceId = form.SelectedDeviceId;

        // No change
        if (newDeviceId == previousDeviceId)
            return;

        // Device unchecked → clear + disconnect
        if (newDeviceId is null)
        {
            _bluetoothService.ClearSelectedDevice();
            if (_isConnected)
                await _bluetoothService.DisconnectAsync();
            UpdateSelectDeviceMenuText();
            return;
        }

        // Different device selected → disconnect old, save new, connect
        if (previousDeviceId is not null && _isConnected)
            await _bluetoothService.DisconnectAsync();

        _bluetoothService.SaveSelectedDevice(newDeviceId, form.SelectedDeviceName ?? "Unknown");
        _bluetoothService.LoadSelectedDevice();
        UpdateSelectDeviceMenuText();

        await _bluetoothService.ConnectAsync();
    }

    private void OnSelectDeviceClicked(object? sender, EventArgs e)
    {
        ShowDevicePicker();
    }

    private void OnConnectionChanged(bool connected)
    {
        _isConnected = connected;

        // Marshal to UI thread
        if (_trayIcon.ContextMenuStrip?.InvokeRequired == true)
        {
            _trayIcon.ContextMenuStrip.Invoke(() => OnConnectionChanged(connected));
            return;
        }

        _trayIcon.Text = connected
            ? $"AirLink - {_bluetoothService.SelectedDeviceName ?? "Device"} (Connected)"
            : "AirLink - Disconnected";

        UpdateIcon();
    }

    private void UpdateIcon()
    {
        _trayIcon.Icon = _iconService.GetIcon(_isConnected, _themeService.IsLightTheme);
    }

    private void UpdateSelectDeviceMenuText()
    {
        _selectDeviceItem.Checked = _bluetoothService.HasSelectedDevice;
        _selectDeviceItem.Text = _bluetoothService.HasSelectedDevice
            ? $"Select Device ({_bluetoothService.SelectedDeviceName})"
            : "Select Device";
    }

    private void OnShortcutClicked(object? sender, EventArgs e)
    {
        using var form = new HotkeyConfigForm(
            _hotkeyService.CurrentModifiers,
            _hotkeyService.CurrentKey,
            _hotkeyService.IsEnabled);

        if (form.ShowDialog() == DialogResult.OK)
        {
            var success = _hotkeyService.ApplyConfig(
                form.ResultModifiers, form.ResultKey, form.ResultEnabled);

            if (form.ResultEnabled && !success)
            {
                MessageBox.Show(
                    $"Could not register {HotkeyService.FormatHotkey(form.ResultModifiers, form.ResultKey)}.\n" +
                    "The shortcut may already be in use by another application.",
                    "AirLink - Shortcut",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            UpdateShortcutMenuText();
        }
    }

    private void UpdateShortcutMenuText()
    {
        _shortcutItem.Checked = _hotkeyService.IsEnabled;
        _shortcutItem.Text = _hotkeyService.IsEnabled
            ? $"Shortcut ({HotkeyService.FormatHotkey(_hotkeyService.CurrentModifiers, _hotkeyService.CurrentKey)})"
            : "Shortcut";
    }

    private void OnHelpClicked(object? sender, EventArgs e)
    {
        var version = typeof(TrayApplicationContext).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";

        MessageBox.Show(
            "AirLink simplifies Bluetooth audio connectivity on Windows.\n" +
            "Originally designed for Apple AirPods, it works with any Bluetooth audio device.\n\n" +
            "Left-click the tray icon to connect or disconnect.\n" +
            "Right-click for options.\n\n" +
            $"Author: Luis Palacios Derqui\n" +
            $"GitHub: {GitHubUrl}\n\n" +
            $"{version}",
            "AirLink - Help",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void OnExitClicked(object? sender, EventArgs e)
    {
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _hotkeyService.Dispose();
            _bluetoothService.Dispose();
            _themeService.Dispose();
        }
        base.Dispose(disposing);
    }
}
