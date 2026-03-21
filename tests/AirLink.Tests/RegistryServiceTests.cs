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
}
