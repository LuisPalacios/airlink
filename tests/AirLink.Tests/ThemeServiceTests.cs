using AirLink.Services;

namespace AirLink.Tests;

public class ThemeServiceTests
{
    [Fact]
    public void ReadThemeFromRegistry_ReturnsBoolean_WithoutThrowing()
    {
        var result = ThemeService.ReadThemeFromRegistry();
        Assert.IsType<bool>(result);
    }
}
