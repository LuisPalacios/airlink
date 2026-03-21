using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;
using Vanara.PInvoke;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using static Vanara.PInvoke.CoreAudio;

namespace AirLink.Services;

public sealed class BluetoothService : IDisposable
{
    private const string DefaultRegistryKey = @"SOFTWARE\AirLink";
    private const string DeviceIdValueName = "DeviceId";
    private const string DeviceNameValueName = "DeviceName";

    private readonly string _registryKeyPath;
    private BluetoothDevice? _currentDevice;

    public bool IsConnected { get; private set; }
    public string? SelectedDeviceId { get; private set; }
    public string? SelectedDeviceName { get; private set; }
    public bool HasSelectedDevice => SelectedDeviceId is not null;

    /// <summary>
    /// Fires when the connection state changes. True = connected, false = disconnected.
    /// </summary>
    public event Action<bool>? ConnectionChanged;

    public BluetoothService() : this(DefaultRegistryKey) { }

    public BluetoothService(string registryKeyPath)
    {
        _registryKeyPath = registryKeyPath;
        LoadSelectedDevice();
    }

    /// <summary>
    /// Returns all paired classic Bluetooth devices.
    /// </summary>
    public async Task<List<DeviceInformation>> FindPairedDevicesAsync()
    {
        var selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        var devices = await DeviceInformation.FindAllAsync(selector);

        var results = new List<DeviceInformation>();
        foreach (var d in devices)
        {
            System.Diagnostics.Debug.WriteLine($"[Paired] {d.Name} | {d.Id}");
            results.Add(d);
        }

        return results;
    }

    /// <summary>
    /// Saves the selected device to the registry and attempts connection.
    /// </summary>
    public async Task<bool> SelectAndConnectAsync(string deviceId, string deviceName)
    {
        SaveSelectedDevice(deviceId, deviceName);
        return await ConnectAsync();
    }

    /// <summary>
    /// Connects to the currently selected device using the KsProperty Bluetooth Audio API.
    /// This is the same mechanism Windows Settings uses — it tells the audio driver
    /// to reconnect all profiles (A2DP + HFP), enabling both speakers and microphone.
    /// </summary>
    public async Task<bool> ConnectAsync()
    {
        if (SelectedDeviceId is null) return false;

        System.Diagnostics.Debug.WriteLine($"Connecting to device: {SelectedDeviceName} ({SelectedDeviceId})");

        try
        {
            // Get the Bluetooth device to monitor connection status and extract container ID
            _currentDevice?.Dispose();
            _currentDevice = await BluetoothDevice.FromIdAsync(SelectedDeviceId);
            if (_currentDevice is null)
            {
                System.Diagnostics.Debug.WriteLine("  FromIdAsync returned null.");
                return false;
            }

            _currentDevice.ConnectionStatusChanged += OnConnectionStatusChanged;

            // Extract MAC for matching audio endpoints to this specific device
            var mac = ExtractBluetoothAddress(SelectedDeviceId);
            var macHex = mac.HasValue ? mac.Value.ToString("X12") : null;

            // Find the audio endpoints associated with this Bluetooth device
            var btAudioDevices = FindBluetoothAudioDevices(macHex, SelectedDeviceName);

            if (btAudioDevices.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("  No audio endpoints found. Trying RFCOMM to register device...");
                var rfcomm = await _currentDevice.GetRfcommServicesAsync(BluetoothCacheMode.Uncached);
                System.Diagnostics.Debug.WriteLine($"  RFCOMM services: {rfcomm.Services.Count}, status: {_currentDevice.ConnectionStatus}");

                await Task.Delay(2000);
                btAudioDevices = FindBluetoothAudioDevices(macHex, SelectedDeviceName);
            }

            if (btAudioDevices.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("  Still no audio endpoints found.");
                return _currentDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;
            }

            // Trigger reconnect on matched audio endpoints only
            foreach (var (name, ksControl) in btAudioDevices)
            {
                System.Diagnostics.Debug.WriteLine($"  Reconnecting: {name}");
                SendBtAudioCommand(ksControl, KSPROPERTY_BTAUDIO.KSPROPERTY_ONESHOT_RECONNECT);
            }

            // KsProperty triggers async connection — wait for it to establish
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(1000);
                if (_currentDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
                {
                    System.Diagnostics.Debug.WriteLine($"  Connected after {i + 1}s");
                    SetConnected(true);
                    return true;
                }
            }

            System.Diagnostics.Debug.WriteLine($"  Connection timed out. Status: {_currentDevice.ConnectionStatus}");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"  Connect failed: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Disconnects the device using the KsProperty Bluetooth Audio API.
    /// This is the same mechanism Windows Settings uses.
    /// </summary>
    public async Task DisconnectAsync()
    {
        System.Diagnostics.Debug.WriteLine($"Disconnecting device: {SelectedDeviceName}");

        try
        {
            var mac = ExtractBluetoothAddress(SelectedDeviceId ?? "");
            var macHex = mac.HasValue ? mac.Value.ToString("X12") : null;
            var btAudioDevices = FindBluetoothAudioDevices(macHex, SelectedDeviceName);

            foreach (var (name, ksControl) in btAudioDevices)
            {
                System.Diagnostics.Debug.WriteLine($"  Disconnecting: {name}");
                SendBtAudioCommand(ksControl, KSPROPERTY_BTAUDIO.KSPROPERTY_ONESHOT_DISCONNECT);
            }

            if (_currentDevice is not null)
            {
                _currentDevice.ConnectionStatusChanged -= OnConnectionStatusChanged;
                _currentDevice.Dispose();
                _currentDevice = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"  Disconnect failed: {ex.Message}");
        }

        SetConnected(false);
    }

    /// <summary>
    /// Synchronous disconnect — only cleans up our references.
    /// For a proper Bluetooth disconnect, use DisconnectAsync().
    /// </summary>
    public void Disconnect()
    {
        try
        {
            var mac = ExtractBluetoothAddress(SelectedDeviceId ?? "");
            var macHex = mac.HasValue ? mac.Value.ToString("X12") : null;
            var btAudioDevices = FindBluetoothAudioDevices(macHex, SelectedDeviceName);

            foreach (var (name, ksControl) in btAudioDevices)
            {
                System.Diagnostics.Debug.WriteLine($"  Disconnecting: {name}");
                SendBtAudioCommand(ksControl, KSPROPERTY_BTAUDIO.KSPROPERTY_ONESHOT_DISCONNECT);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"  KsProperty disconnect failed: {ex.Message}");
        }

        if (_currentDevice is not null)
        {
            _currentDevice.ConnectionStatusChanged -= OnConnectionStatusChanged;
            _currentDevice.Dispose();
            _currentDevice = null;
        }

        SetConnected(false);
    }

    /// <summary>
    /// Clears the selected device from registry. Does NOT disconnect.
    /// </summary>
    public void ClearSelectedDevice()
    {
        SelectedDeviceId = null;
        SelectedDeviceName = null;

        using var key = Registry.CurrentUser.OpenSubKey(_registryKeyPath, true);
        if (key is not null)
        {
            key.DeleteValue(DeviceIdValueName, throwOnMissingValue: false);
            key.DeleteValue(DeviceNameValueName, throwOnMissingValue: false);
        }
    }

    // --- KsProperty Bluetooth Audio API ---

    /// <summary>
    /// Finds Bluetooth audio endpoints matching the given MAC address
    /// by walking the CoreAudio device topology.
    /// </summary>
    /// <param name="macHex">MAC address as uppercase hex (e.g., "C435D90FF883"), or null to match all.</param>
    /// <param name="deviceName">Device name for fallback matching on HFP endpoints that lack MAC in path.</param>
    private static List<(string Name, IKsControl KsControl)> FindBluetoothAudioDevices(string? macHex, string? deviceName = null)
    {
        var results = new List<(string, IKsControl)>();
        System.Diagnostics.Debug.WriteLine($"  Searching audio endpoints (MAC filter: {macHex ?? "none"}, name: {deviceName ?? "none"})");

        try
        {
            var enumerator = new IMMDeviceEnumerator();
            var deviceCollection = enumerator.EnumAudioEndpoints(EDataFlow.eAll, DEVICE_STATE.DEVICE_STATEMASK_ALL);

            for (uint i = 0; i < deviceCollection.GetCount(); i++)
            {
                deviceCollection.Item(i, out var mmDevice);

                try
                {
                    mmDevice!.Activate(typeof(IDeviceTopology).GUID, Ole32.CLSCTX.CLSCTX_ALL, null, out var topoObj);
                    if (topoObj is not IDeviceTopology topology) continue;

                    for (uint c = 0; c < topology.GetConnectorCount(); c++)
                    {
                        var connector = topology.GetConnector(c);
                        IConnector? connectedTo;
                        try
                        {
                            connectedTo = connector.GetConnectedTo();
                        }
                        catch
                        {
                            continue;
                        }

                        if (connectedTo is not IPart connectedToPart) continue;

                        var connectedToDeviceId = (string?)connectedToPart.GetTopologyObject()?.GetDeviceId();
                        if (connectedToDeviceId is null ||
                            !connectedToDeviceId.StartsWith(@"{2}.\\?\bth", StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Get friendly name for matching and display
                        var propStore = mmDevice.OpenPropertyStore(STGM.STGM_READ);
                        var name = (string?)propStore?.GetValue(DeviceProperties.PKEY_Device_FriendlyName) ?? "Unknown";

                        // Match this endpoint to our device
                        if (macHex is not null || deviceName is not null)
                        {
                            var idUpper = connectedToDeviceId.ToUpperInvariant().Replace(":", "");
                            var matchedByMac = macHex is not null && idUpper.Contains(macHex);
                            // For bthhfenum paths (HFP/mic) that don't contain MAC, match by device name
                            var matchedByName = !matchedByMac && deviceName is not null &&
                                name.Contains(deviceName, StringComparison.OrdinalIgnoreCase);

                            if (!matchedByMac && !matchedByName)
                            {
                                System.Diagnostics.Debug.WriteLine($"  Skipping (no match): {name} | {connectedToDeviceId}");
                                continue;
                            }
                        }

                        // Get IKsControl from the connected Bluetooth device
                        var connectedToDevice = enumerator.GetDevice(connectedToDeviceId);
                        if (connectedToDevice is null) continue;

                        connectedToDevice.Activate(typeof(IKsControl).GUID, Ole32.CLSCTX.CLSCTX_ALL, null, out var ksObj);
                        if (ksObj is not IKsControl ksControl) continue;

                        results.Add((name, ksControl));
                        System.Diagnostics.Debug.WriteLine($"  Matched: {name} | {connectedToDeviceId}");
                    }
                }
                catch
                {
                    // Skip endpoints without topology or with errors
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"  Audio endpoint enumeration failed: {ex.Message}");
        }

        return results;
    }

    private static void SendBtAudioCommand(IKsControl ksControl, KSPROPERTY_BTAUDIO command)
    {
        var ksProperty = new KsProperty(
            KsPropertyId.KSPROPSETID_BtAudio,
            command,
            KsPropertyKind.KSPROPERTY_TYPE_GET);

        var dwReturned = 0;
        var hr = ksControl.KsProperty(
            ref ksProperty,
            Marshal.SizeOf(ksProperty),
            IntPtr.Zero,
            0,
            ref dwReturned);

        System.Diagnostics.Debug.WriteLine($"    KsProperty result: 0x{hr:X8}");
    }

    /// <summary>
    /// Checks if the selected device is currently connected and sets up monitoring.
    /// Call on startup to detect pre-existing connections.
    /// </summary>
    public async Task CheckConnectionStatusAsync()
    {
        if (SelectedDeviceId is null) return;

        try
        {
            _currentDevice = await BluetoothDevice.FromIdAsync(SelectedDeviceId);
            if (_currentDevice is not null)
            {
                _currentDevice.ConnectionStatusChanged += OnConnectionStatusChanged;
                var connected = _currentDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;
                System.Diagnostics.Debug.WriteLine($"Startup check: {_currentDevice.Name} is {_currentDevice.ConnectionStatus}");
                SetConnected(connected);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Startup connection check failed: {ex.Message}");
        }
    }

    // --- Registry persistence ---

    public void LoadSelectedDevice()
    {
        LoadSelectedDevice(_registryKeyPath);
    }

    public void LoadSelectedDevice(string registryKeyPath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(registryKeyPath, false);
        SelectedDeviceId = key?.GetValue(DeviceIdValueName) as string;
        SelectedDeviceName = key?.GetValue(DeviceNameValueName) as string;
    }

    public void SaveSelectedDevice(string deviceId, string deviceName)
    {
        SaveSelectedDevice(deviceId, deviceName, _registryKeyPath);
    }

    public static void SaveSelectedDevice(string deviceId, string deviceName, string registryKeyPath)
    {
        using var key = Registry.CurrentUser.CreateSubKey(registryKeyPath);
        key.SetValue(DeviceIdValueName, deviceId, RegistryValueKind.String);
        key.SetValue(DeviceNameValueName, deviceName, RegistryValueKind.String);
    }

    // --- Event handlers ---

    private void OnConnectionStatusChanged(BluetoothDevice sender, object args)
    {
        var connected = sender.ConnectionStatus == BluetoothConnectionStatus.Connected;
        System.Diagnostics.Debug.WriteLine($"[ConnectionStatusChanged] {sender.Name}: {sender.ConnectionStatus}");
        SetConnected(connected);
    }

    private void SetConnected(bool connected)
    {
        if (IsConnected != connected)
        {
            IsConnected = connected;
            ConnectionChanged?.Invoke(connected);
        }
    }

    public static ulong? ExtractBluetoothAddress(string deviceId)
    {
        var dashIdx = deviceId.LastIndexOf('-');
        if (dashIdx < 0) return null;

        var macStr = deviceId[(dashIdx + 1)..].Replace(":", "");
        if (ulong.TryParse(macStr, System.Globalization.NumberStyles.HexNumber, null, out var address))
            return address;

        return null;
    }

    public void Dispose()
    {
        if (_currentDevice is not null)
        {
            _currentDevice.ConnectionStatusChanged -= OnConnectionStatusChanged;
            _currentDevice.Dispose();
            _currentDevice = null;
        }
    }

    // --- KsProperty interop definitions ---

    private static class DeviceProperties
    {
        public static Ole32.PROPERTYKEY PKEY_Device_FriendlyName =
            new(new("{a45c254e-df1c-4efd-8020-67d146a850e0}"), 14u);
    }

    private enum KsPropertyKind : uint
    {
        KSPROPERTY_TYPE_GET = 0x00000001,
    }

    private enum KSPROPERTY_BTAUDIO : uint
    {
        KSPROPERTY_ONESHOT_RECONNECT = 0,
        KSPROPERTY_ONESHOT_DISCONNECT = 1,
    }

    private static class KsPropertyId
    {
        public static readonly Guid KSPROPSETID_BtAudio = new("7fa06c40-b8f6-4c7e-8556-e8c33a12e54d");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KsProperty(Guid set, KSPROPERTY_BTAUDIO id, KsPropertyKind flags)
    {
        public Guid Set = set;
        public KSPROPERTY_BTAUDIO Id = id;
        public KsPropertyKind Flags = flags;
    }

    [ComImport, SuppressUnmanagedCodeSecurity,
     Guid("28F54685-06FD-11D2-B27A-00A0C9223196"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IKsControl
    {
        [PreserveSig]
        int KsProperty(
            [In] ref KsProperty Property,
            [In] int PropertyLength,
            [In, Out] IntPtr PropertyData,
            [In] int DataLength,
            [In, Out] ref int BytesReturned);

        [PreserveSig]
        int KsMethod(
            [In] ref KsProperty Method,
            [In] int MethodLength,
            [In, Out] IntPtr MethodData,
            [In] int DataLength,
            [In, Out] ref int BytesReturned);

        [PreserveSig]
        int KsEvent(
            [In] ref KsProperty Event,
            [In] int EventLength,
            [In, Out] IntPtr EventData,
            [In] int DataLength,
            [In, Out] ref int BytesReturned);
    }
}
