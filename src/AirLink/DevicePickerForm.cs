using Vanara.PInvoke;
using Windows.Devices.Enumeration;
using static Vanara.PInvoke.CoreAudio;

namespace AirLink;

/// <summary>
/// Modal dialog that lists paired Bluetooth audio devices for user selection.
/// Shows colored dots for mic/audio status and bolds AirPods entries.
/// </summary>
public sealed class DevicePickerForm : Form
{
    private readonly ListView _deviceList;
    private readonly Button _okButton;
    private readonly Label _statusLabel;
    private readonly System.Windows.Forms.Timer _refreshTimer;

    // The device ID/name that has the checkmark — starts as current stored device
    private string? _checkedDeviceId;
    private string? _checkedDeviceName;

    // Remember position across opens (in-memory only)
    private static Point? _lastLocation;

    /// <summary>The device ID to save (or null to clear).</summary>
    public string? SelectedDeviceId { get; private set; }
    public string? SelectedDeviceName { get; private set; }

    // Audio endpoint info per device MAC
    private readonly record struct AudioStatus(bool HasAudio, bool AudioActive, bool HasMic, bool MicActive);
    private Dictionary<string, AudioStatus> _audioStatusByMac = new();

    public DevicePickerForm(string? currentDeviceName, string? currentDeviceId)
    {
        _checkedDeviceId = currentDeviceId;
        _checkedDeviceName = currentDeviceName;

        Text = "AirLink - Select Bluetooth Device";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = _lastLocation.HasValue ? FormStartPosition.Manual : FormStartPosition.CenterScreen;
        if (_lastLocation.HasValue) Location = _lastLocation.Value;
        Size = new Size(400, 340);

        _okButton = new Button
        {
            Text = "OK",
            Location = new Point(292, 260),
            Size = new Size(80, 28),
        };
        _okButton.Click += (_, _) =>
        {
            SelectedDeviceId = _checkedDeviceId;
            SelectedDeviceName = _checkedDeviceName;
            _lastLocation = Location;
            DialogResult = DialogResult.OK;
            Close();
        };

        _deviceList = new DoubleBufferedListView
        {
            Location = new Point(16, 16),
            Size = new Size(350, 210),
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            HeaderStyle = ColumnHeaderStyle.None,
            OwnerDraw = true,
        };
        _deviceList.Columns.Add("Device Name", 330, HorizontalAlignment.Left);
        _deviceList.MouseClick += OnDeviceListClick;
        _deviceList.DrawItem += OnDrawItem;

        _statusLabel = new Label
        {
            Text = "Scanning for paired devices...",
            Location = new Point(16, 232),
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
        };

        AcceptButton = _okButton;

        Controls.AddRange([_deviceList, _statusLabel, _okButton]);

        // Refresh audio status dots every 2 seconds while the dialog is open
        _refreshTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        _refreshTimer.Tick += (_, _) =>
        {
            var newStatus = GetBluetoothAudioStatus();
            // Only repaint if something actually changed
            if (!AudioStatusEqual(_audioStatusByMac, newStatus))
            {
                _audioStatusByMac = newStatus;
                _deviceList.Invalidate();
            }
        };
        _refreshTimer.Start();
    }

    private static bool AudioStatusEqual(
        Dictionary<string, AudioStatus> a, Dictionary<string, AudioStatus> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var (key, val) in a)
        {
            if (!b.TryGetValue(key, out var other) || val != other)
                return false;
        }
        return true;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _lastLocation = Location;
        _refreshTimer.Stop();
        _refreshTimer.Dispose();
        base.OnFormClosed(e);
    }

    private void OnDeviceListClick(object? sender, MouseEventArgs e)
    {
        var item = _deviceList.GetItemAt(e.X, e.Y);
        if (item is null) return;

        var deviceId = item.Tag as string;
        var deviceName = item.Text;

        // Toggle: click same device unchecks it, click different device checks it
        if (_checkedDeviceId == deviceId)
        {
            _checkedDeviceId = null;
            _checkedDeviceName = null;
        }
        else
        {
            _checkedDeviceId = deviceId;
            _checkedDeviceName = deviceName;
        }

        _deviceList.Invalidate();
    }

    private void OnDrawItem(object? sender, DrawListViewItemEventArgs e)
    {
        var isSelected = e.Item.Selected;
        var name = e.Item.Text;
        var deviceId = e.Item.Tag as string ?? "";
        var isAirPods = name.Contains("AirPods", StringComparison.OrdinalIgnoreCase);
        var isChecked = _checkedDeviceId is not null && _checkedDeviceId == deviceId;

        // Background
        e.Graphics.FillRectangle(
            isSelected ? SystemBrushes.Highlight : SystemBrushes.Window,
            e.Bounds);

        var centerY = e.Bounds.Top + e.Bounds.Height / 2;
        var x = e.Bounds.Left + 4;

        // Status dots (audio = blue/gray, mic = red/gray)
        var extractedMac = Services.BluetoothService.ExtractBluetoothAddress(deviceId);
        var macHex = extractedMac.HasValue ? extractedMac.Value.ToString("X12") : "";
        _audioStatusByMac.TryGetValue(macHex, out var status);

        using (var audioBrush = new SolidBrush(status.AudioActive ? Color.DodgerBlue : Color.LightGray))
            e.Graphics.FillEllipse(audioBrush, x, centerY - 4, 8, 8);

        x += 12;

        using (var micBrush = new SolidBrush(status.MicActive ? Color.Tomato : Color.LightGray))
            e.Graphics.FillEllipse(micBrush, x, centerY - 4, 8, 8);

        x += 14;

        // Device name (bold for AirPods)
        var fontStyle = isAirPods ? FontStyle.Bold : FontStyle.Regular;
        using var font = new Font(_deviceList.Font, fontStyle);
        var textColor = isSelected ? SystemColors.HighlightText : SystemColors.ControlText;

        // Reserve space for the checkmark on the right
        var checkSize = 14;
        var checkRight = e.Bounds.Right - 8;
        var textRect = new Rectangle(x, e.Bounds.Top, checkRight - checkSize - 8 - x, e.Bounds.Height);

        TextRenderer.DrawText(e.Graphics, name, font, textRect, textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        // Green checkmark (far right) for stored device, empty box for others
        var checkRect = new Rectangle(checkRight - checkSize, centerY - 7, checkSize, checkSize);
        if (isChecked)
        {
            using var checkBrush = new SolidBrush(Color.ForestGreen);
            e.Graphics.FillRectangle(checkBrush, checkRect);
            using var checkFont = new Font("Segoe UI", 8f, FontStyle.Bold);
            var checkTextRect = new Rectangle(checkRect.X, checkRect.Y - 2, checkRect.Width, checkRect.Height);
            TextRenderer.DrawText(e.Graphics, "\u2713", checkFont, checkTextRect, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
        else
        {
            using var pen = new Pen(Color.LightGray, 1);
            e.Graphics.DrawRectangle(pen, checkRect);
        }

        e.DrawFocusRectangle();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await LoadDevicesAsync();
    }

    private async Task LoadDevicesAsync()
    {
        try
        {
            var service = new Services.BluetoothService();
            var devices = await service.FindPairedDevicesAsync();
            service.Dispose();

            // Collect audio endpoint info per MAC
            _audioStatusByMac = GetBluetoothAudioStatus();

            _deviceList.Items.Clear();

            var audioDevices = devices
                .Where(d =>
                {
                    var mac = Services.BluetoothService.ExtractBluetoothAddress(d.Id);
                    return mac.HasValue && _audioStatusByMac.ContainsKey(mac.Value.ToString("X12"));
                })
                .ToList();

            // Move AirPods devices to the top, keep original order otherwise
            audioDevices = audioDevices
                .OrderByDescending(d => d.Name.Contains("AirPods", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (audioDevices.Count == 0)
            {
                _statusLabel.Text = "No paired Bluetooth audio devices found.";
                return;
            }

            foreach (var device in audioDevices)
            {
                var item = new ListViewItem(device.Name)
                {
                    Tag = device.Id,
                };
                _deviceList.Items.Add(item);
            }

            _statusLabel.Text = $"{audioDevices.Count} audio device(s) found.";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Error scanning: {ex.Message}";
        }
    }


    /// <summary>
    /// Collects audio status (has audio/mic, active/inactive) per Bluetooth MAC address
    /// by walking the CoreAudio device topology.
    /// </summary>
    private static Dictionary<string, AudioStatus> GetBluetoothAudioStatus()
    {
        var result = new Dictionary<string, AudioStatus>();
        // Map device names to MACs (from bthenum endpoints that have MAC in path)
        var nameToMac = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // Deferred bthhfenum endpoints (no MAC in path, need name-based matching)
        var deferred = new List<(string Name, bool IsActive)>();

        try
        {
            var enumerator = new IMMDeviceEnumerator();
            var devices = enumerator.EnumAudioEndpoints(EDataFlow.eAll, DEVICE_STATE.DEVICE_STATEMASK_ALL);

            for (uint i = 0; i < devices.GetCount(); i++)
            {
                devices.Item(i, out var mmDevice);
                try
                {
                    var state = mmDevice!.GetState();
                    var isActive = state == DEVICE_STATE.DEVICE_STATE_ACTIVE;

                    mmDevice.Activate(typeof(IDeviceTopology).GUID, Ole32.CLSCTX.CLSCTX_ALL, null, out var topoObj);
                    if (topoObj is not IDeviceTopology topology) continue;

                    var propStore = mmDevice.OpenPropertyStore(STGM.STGM_READ);
                    var name = (string?)propStore?.GetValue(
                        new Ole32.PROPERTYKEY(new("{a45c254e-df1c-4efd-8020-67d146a850e0}"), 14u)) ?? "";

                    for (uint c = 0; c < topology.GetConnectorCount(); c++)
                    {
                        var connector = topology.GetConnector(c);
                        try
                        {
                            var connectedTo = connector.GetConnectedTo();
                            if (connectedTo is not IPart part) continue;

                            var deviceId = (string?)part.GetTopologyObject()?.GetDeviceId();
                            if (deviceId is null ||
                                !deviceId.StartsWith(@"{2}.\\?\bth", StringComparison.OrdinalIgnoreCase))
                                continue;

                            // Try to extract MAC from bthenum path
                            var mac = ExtractMacFromDevicePath(deviceId);

                            if (mac is not null)
                            {
                                // bthenum endpoint — has MAC, this is audio output (A2DP)
                                // Extract the device name from friendly name like "Headphones (AirPods Pro)"
                                var deviceName = ExtractDeviceNameFromEndpoint(name);
                                if (deviceName is not null)
                                    nameToMac.TryAdd(deviceName, mac);

                                if (!result.TryGetValue(mac, out var existing))
                                    existing = new AudioStatus();

                                result[mac] = new AudioStatus(
                                    HasAudio: true,
                                    AudioActive: existing.AudioActive || isActive,
                                    HasMic: existing.HasMic,
                                    MicActive: existing.MicActive
                                );
                            }
                            else
                            {
                                // bthhfenum endpoint — no MAC, this is mic (HFP)
                                deferred.Add((name, isActive));
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }
        catch { }

        // Match deferred bthhfenum (mic) endpoints to MACs via device name
        foreach (var (name, isActive) in deferred)
        {
            var deviceName = ExtractDeviceNameFromEndpoint(name);
            if (deviceName is null) continue;

            // Find a MAC that has a matching device name
            if (!nameToMac.TryGetValue(deviceName, out var mac)) continue;

            if (!result.TryGetValue(mac, out var existing))
                existing = new AudioStatus();

            result[mac] = new AudioStatus(
                HasAudio: existing.HasAudio,
                AudioActive: existing.AudioActive,
                HasMic: true,
                MicActive: existing.MicActive || isActive
            );
        }

        return result;
    }

    /// <summary>
    /// Extracts MAC hex string from bthenum device paths like ...&amp;0&amp;C435D90FF883_...
    /// Returns null for bthhfenum paths that don't contain MAC.
    /// </summary>
    private static string? ExtractMacFromDevicePath(string deviceId)
    {
        var idUpper = deviceId.ToUpperInvariant();
        var marker = "&0&";
        var idx = idUpper.LastIndexOf(marker);
        if (idx < 0) return null;

        var afterMarker = idUpper[(idx + marker.Length)..];
        var endIdx = afterMarker.IndexOf('_');
        if (endIdx < 0) return null;

        var mac = afterMarker[..endIdx];
        return mac.Length >= 12 ? mac : null;
    }

    /// <summary>
    /// Extracts the device name from an endpoint friendly name.
    /// E.g., "Headphones (AirPods Pro - Find My)" → "AirPods Pro - Find My"
    /// E.g., "Headset (WH-1000XM6)" → "WH-1000XM6"
    /// </summary>
    private static string? ExtractDeviceNameFromEndpoint(string endpointName)
    {
        var start = endpointName.IndexOf('(');
        var end = endpointName.LastIndexOf(')');
        if (start < 0 || end <= start) return null;
        return endpointName[(start + 1)..end];
    }

    /// <summary>
    /// ListView with double buffering enabled to eliminate flicker on repaint.
    /// </summary>
    private sealed class DoubleBufferedListView : ListView
    {
        public DoubleBufferedListView()
        {
            DoubleBuffered = true;
        }
    }
}
