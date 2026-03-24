using AirLink.Services;
using Microsoft.Win32;

namespace AirLink.Tests;

public class RegistryServiceTests : IDisposable
{
    private const string TestRunKey = @"SOFTWARE\AirLink\Tests\Run";
    private const string TestAppName = "AirLinkTest";

    public RegistryServiceTests()
    {
        // Ensure the test key exists
        Registry.CurrentUser.CreateSubKey(TestRunKey)?.Dispose();
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
    public void IsStartupEnabled_ReturnsFalse_WhenNotSet()
    {
        Assert.False(RegistryService.IsStartupEnabled(TestRunKey, TestAppName));
    }

    [Fact]
    public void SetStartupEnabled_EnableThenDisable_RoundTrips()
    {
        RegistryService.SetStartupEnabled(true, TestRunKey, TestAppName);
        Assert.True(RegistryService.IsStartupEnabled(TestRunKey, TestAppName));

        RegistryService.SetStartupEnabled(false, TestRunKey, TestAppName);
        Assert.False(RegistryService.IsStartupEnabled(TestRunKey, TestAppName));
    }

    [Fact]
    public void SetStartupEnabled_DisableWhenAlreadyDisabled_DoesNotThrow()
    {
        RegistryService.SetStartupEnabled(false, TestRunKey, TestAppName);
        Assert.False(RegistryService.IsStartupEnabled(TestRunKey, TestAppName));
    }

    [Fact]
    public void IsStartupPathCurrent_ReturnsTrue_WhenNoKeyExists()
    {
        Assert.True(RegistryService.IsStartupPathCurrent(TestRunKey, TestAppName));
    }

    [Fact]
    public void IsStartupPathCurrent_ReturnsTrue_AfterSetEnabled()
    {
        // SetStartupEnabled writes the current process path
        RegistryService.SetStartupEnabled(true, TestRunKey, TestAppName);
        Assert.True(RegistryService.IsStartupPathCurrent(TestRunKey, TestAppName));
    }

    [Fact]
    public void IsStartupPathCurrent_ReturnsFalse_WhenPathDiffers()
    {
        // Write a fake path that doesn't match the running process
        using var key = Registry.CurrentUser.OpenSubKey(TestRunKey, true)!;
        key.SetValue(TestAppName, @"""C:\old\location\AirLink.exe""");
        Assert.False(RegistryService.IsStartupPathCurrent(TestRunKey, TestAppName));
    }
}
