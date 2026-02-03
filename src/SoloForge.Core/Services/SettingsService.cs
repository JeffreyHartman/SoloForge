using System.Text.Json;

namespace SoloForge.Console.Services;

/// <summary>
/// Provides access to application settings from appsettings.json.
/// </summary>
public sealed class SettingsService
{
    private static readonly Lazy<SettingsService> _instance = new(() => new SettingsService());
    public static SettingsService Instance => _instance.Value;

    public FeatureSettings Features { get; }

    private SettingsService()
    {
        Features = LoadSettings();
    }

    private static FeatureSettings LoadSettings()
    {
        var settingsPath = FindSettingsFile();
        if (settingsPath == null || !File.Exists(settingsPath))
        {
            return new FeatureSettings();
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return settings?.Features ?? new FeatureSettings();
        }
        catch
        {
            return new FeatureSettings();
        }
    }

    private static string? FindSettingsFile()
    {
        // Try to find appsettings.json relative to executable
        var baseDir = AppContext.BaseDirectory;
        var currentDir = new DirectoryInfo(baseDir);

        while (currentDir != null)
        {
            var settingsPath = Path.Combine(currentDir.FullName, "appsettings.json");
            if (File.Exists(settingsPath))
                return settingsPath;

            // Also check src/SoloForge.Console for development
            var srcPath = Path.Combine(currentDir.FullName, "src", "SoloForge.Console", "appsettings.json");
            if (File.Exists(srcPath))
                return srcPath;

            currentDir = currentDir.Parent;
        }

        return null;
    }
}

public record AppSettings
{
    public FeatureSettings Features { get; init; } = new();
}

public record FeatureSettings
{
    public bool ShowSubpageTitles { get; init; } = true;
}
