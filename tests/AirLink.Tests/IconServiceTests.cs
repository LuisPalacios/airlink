using AirLink.Services;

namespace AirLink.Tests;

public class IconServiceTests
{
    [Theory]
    [InlineData(true, true, "AirLink.Icons.app-connected-lighttheme.ico")]
    [InlineData(true, false, "AirLink.Icons.app-connected-darktheme.ico")]
    [InlineData(false, true, "AirLink.Icons.app-disconnected-lighttheme.ico")]
    [InlineData(false, false, "AirLink.Icons.app-disconnected-darktheme.ico")]
    public void GetResourceName_ReturnsCorrectName(bool connected, bool isLight, string expected)
    {
        Assert.Equal(expected, IconService.GetResourceName(connected, isLight));
    }
}
