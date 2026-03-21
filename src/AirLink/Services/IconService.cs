using System.Reflection;

namespace AirLink.Services;

public sealed class IconService
{
    private readonly Dictionary<string, Icon> _cache = new();

    /// <summary>
    /// Returns the appropriate tray icon based on connection state and OS theme.
    /// Icons are loaded from embedded resources and cached.
    /// </summary>
    public Icon GetIcon(bool connected, bool isLightTheme)
    {
        var resourceName = GetResourceName(connected, isLightTheme);

        if (!_cache.TryGetValue(resourceName, out var icon))
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
            icon = new Icon(stream);
            _cache[resourceName] = icon;
        }

        return icon;
    }

    /// <summary>
    /// Builds the embedded resource logical name for the given state.
    /// </summary>
    public static string GetResourceName(bool connected, bool isLightTheme)
    {
        var state = connected ? "connected" : "disconnected";
        var theme = isLightTheme ? "lighttheme" : "darktheme";
        return $"AirLink.Icons.app-{state}-{theme}.ico";
    }
}
