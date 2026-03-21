using AirLink.Services;
using Microsoft.Win32;

namespace AirLink.Tests;

public class BluetoothServiceTests : IDisposable
{
    private const string TestRegistryKey = @"SOFTWARE\AirLink\Tests\Bluetooth";

    public BluetoothServiceTests()
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
    public void LoadSelectedDevice_WhenEmpty_ReturnsNull()
    {
        using var service = new BluetoothService(@"SOFTWARE\AirLink\Tests\NonExistent");
        Assert.Null(service.SelectedDeviceId);
        Assert.Null(service.SelectedDeviceName);
        Assert.False(service.HasSelectedDevice);
    }

    [Fact]
    public void SaveAndLoadSelectedDevice_RoundTrips()
    {
        var testId = "Bluetooth#Device_abc123";
        var testName = "AirPods Pro";

        BluetoothService.SaveSelectedDevice(testId, testName, TestRegistryKey);

        using var service = new BluetoothService(TestRegistryKey);
        Assert.Equal(testId, service.SelectedDeviceId);
        Assert.Equal(testName, service.SelectedDeviceName);
        Assert.True(service.HasSelectedDevice);
    }

    [Fact]
    public void ClearSelectedDevice_RemovesFromRegistry()
    {
        BluetoothService.SaveSelectedDevice("id1", "Device1", TestRegistryKey);

        using var service = new BluetoothService(TestRegistryKey);
        Assert.True(service.HasSelectedDevice);

        service.ClearSelectedDevice();
        Assert.False(service.HasSelectedDevice);
        Assert.Null(service.SelectedDeviceId);
        Assert.Null(service.SelectedDeviceName);

        // Verify registry is also cleared
        using var service2 = new BluetoothService(TestRegistryKey);
        Assert.False(service2.HasSelectedDevice);
    }
}
