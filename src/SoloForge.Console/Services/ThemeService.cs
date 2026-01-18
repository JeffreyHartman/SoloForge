using System.Text.Json;
using Terminal.Gui;
using SoloForge.Console.Models;

namespace SoloForge.Console.Services;

/// <summary>
/// Service for loading and applying themes from themes.json.
/// Provides ColorScheme objects based on the active theme.
/// </summary>
public sealed class ThemeService
{
    private static readonly Lazy<ThemeService> _instance = new(() => new ThemeService());
    public static ThemeService Instance => _instance.Value;

    private readonly List<ThemeDefinition> _themes;
    private ThemeDefinition _activeTheme;

    // Cached ColorSchemes for the active theme
    private ColorScheme _default = null!;
    private ColorScheme _menu = null!;
    private ColorScheme _primary = null!;
    private ColorScheme _accent = null!;
    private ColorScheme _success = null!;
    private ColorScheme _failure = null!;
    private ColorScheme _warning = null!;
    private ColorScheme _muted = null!;
    private ColorScheme _error = null!;

    /// <summary>
    /// Event fired when the theme changes.
    /// </summary>
    public event Action? ThemeChanged;

    private ThemeService()
    {
        _themes = LoadThemes();
        _activeTheme = _themes.FirstOrDefault() ?? GetFallbackTheme();
        BuildColorSchemes();
    }

    /// <summary>
    /// List of available theme names.
    /// </summary>
    public IReadOnlyList<string> AvailableThemes => _themes.Select(t => t.Name).ToList();

    /// <summary>
    /// List of all theme definitions.
    /// </summary>
    public IReadOnlyList<ThemeDefinition> Themes => _themes;

    /// <summary>
    /// The name of the currently active theme.
    /// </summary>
    public string ActiveThemeName => _activeTheme.Name;

    // ColorScheme properties
    public ColorScheme Default => _default;
    public ColorScheme Menu => _menu;
    public ColorScheme Primary => _primary;
    public ColorScheme Accent => _accent;
    public ColorScheme Success => _success;
    public ColorScheme Failure => _failure;
    public ColorScheme Warning => _warning;
    public ColorScheme Muted => _muted;
    public ColorScheme Error => _error;

    /// <summary>
    /// Applies a theme by name.
    /// </summary>
    /// <param name="themeName">The name of the theme to apply.</param>
    /// <returns>True if the theme was found and applied.</returns>
    public bool ApplyTheme(string themeName)
    {
        var theme = _themes.FirstOrDefault(t =>
            t.Name.Equals(themeName, StringComparison.OrdinalIgnoreCase));

        if (theme == null)
        {
            return false;
        }

        _activeTheme = theme;
        BuildColorSchemes();
        ThemeChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Applies a theme and saves to settings.
    /// </summary>
    public bool ApplyAndSaveTheme(string themeName)
    {
        if (!ApplyTheme(themeName))
        {
            return false;
        }

        SaveSettings();
        return true;
    }

    /// <summary>
    /// Loads settings from GlobalSettings.json and applies them.
    /// </summary>
    public void LoadSettings()
    {
        var settingsPath = GetSettingsPath();
        if (!File.Exists(settingsPath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            var settings = JsonSerializer.Deserialize<GlobalSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (settings != null && !string.IsNullOrEmpty(settings.ThemeName))
            {
                ApplyTheme(settings.ThemeName);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Logger.Warning(ex, "Failed to load theme settings");
        }
    }

    /// <summary>
    /// Saves current theme settings to GlobalSettings.json.
    /// </summary>
    public void SaveSettings()
    {
        var settingsPath = GetSettingsPath();

        try
        {
            GlobalSettings settings;

            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                settings = JsonSerializer.Deserialize<GlobalSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new GlobalSettings();
            }
            else
            {
                settings = new GlobalSettings();
            }

            settings.ThemeName = _activeTheme.Name;

            var dir = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var output = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            File.WriteAllText(settingsPath, output);
        }
        catch (Exception ex)
        {
            AppLogger.Logger.Warning(ex, "Failed to save theme settings");
        }
    }

    /// <summary>
    /// Returns a ColorScheme for fate check results based on outcome.
    /// </summary>
    public ColorScheme ForFateResult(string? result)
    {
        if (string.IsNullOrEmpty(result))
        {
            return _failure;
        }
        return result.Contains("Yes") ? _success : _failure;
    }

    /// <summary>
    /// Returns a ColorScheme for scene check results.
    /// </summary>
    public ColorScheme ForSceneResult(string? result) => result switch
    {
        "Normal Scene" => _success,
        "Altered Scene!" => _warning,
        "Interrupt Scene!" => _failure,
        null => _default,
        _ => _default
    };

    /// <summary>
    /// Returns a ColorScheme for chaos factor display.
    /// </summary>
    public ColorScheme ForChaos(int chaos) => chaos switch
    {
        <= 3 => _success,
        <= 6 => _warning,
        _ => _failure
    };

    private List<ThemeDefinition> LoadThemes()
    {
        var themesPath = FindThemesFile();
        if (themesPath == null || !File.Exists(themesPath))
        {
            AppLogger.Logger.Warning("themes.json not found, using fallback theme");
            return [GetFallbackTheme()];
        }

        try
        {
            var json = File.ReadAllText(themesPath);
            var collection = JsonSerializer.Deserialize<ThemeCollection>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (collection?.Themes == null || collection.Themes.Count == 0)
            {
                AppLogger.Logger.Warning("No themes found in themes.json, using fallback");
                return [GetFallbackTheme()];
            }

            return collection.Themes;
        }
        catch (Exception ex)
        {
            AppLogger.Logger.Error(ex, "Failed to load themes.json, using fallback");
            return [GetFallbackTheme()];
        }
    }

    private static string? FindThemesFile()
    {
        var baseDir = AppContext.BaseDirectory;
        var currentDir = new DirectoryInfo(baseDir);

        while (currentDir != null)
        {
            var themesPath = Path.Combine(currentDir.FullName, "data", "themes.json");
            if (File.Exists(themesPath))
            {
                return themesPath;
            }

            currentDir = currentDir.Parent;
        }

        // Fallback to relative path
        var relativePath = Path.Combine(Directory.GetCurrentDirectory(), "data", "themes.json");
        return File.Exists(relativePath) ? relativePath : null;
    }

    private static string GetSettingsPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var currentDir = new DirectoryInfo(baseDir);

        while (currentDir != null)
        {
            var savesPath = Path.Combine(currentDir.FullName, "saves");
            if (Directory.Exists(savesPath))
            {
                return Path.Combine(savesPath, "GlobalSettings.json");
            }
            currentDir = currentDir.Parent;
        }

        // Fallback to creating saves directory
        var fallbackSaves = Path.Combine(Directory.GetCurrentDirectory(), "saves");
        return Path.Combine(fallbackSaves, "GlobalSettings.json");
    }

    private void BuildColorSchemes()
    {
        _default = BuildColorScheme(_activeTheme.Default);
        _menu = BuildColorScheme(_activeTheme.Menu);
        _primary = BuildColorScheme(_activeTheme.Primary);
        _accent = BuildColorScheme(_activeTheme.Accent);
        _success = BuildColorScheme(_activeTheme.Success);
        _failure = BuildColorScheme(_activeTheme.Failure);
        _warning = BuildColorScheme(_activeTheme.Warning);
        _muted = BuildColorScheme(_activeTheme.Muted);
        _error = BuildColorScheme(_activeTheme.Error);
    }

    private static ColorScheme BuildColorScheme(ColorSchemeDefinition def)
    {
        return new ColorScheme
        {
            Normal = BuildAttribute(def.Normal),
            Focus = BuildAttribute(def.Focus),
            HotNormal = BuildAttribute(def.HotNormal),
            HotFocus = BuildAttribute(def.HotFocus),
            Disabled = BuildAttribute(def.Disabled)
        };
    }

    private static Terminal.Gui.Attribute BuildAttribute(ColorAttributeDefinition def)
    {
        var fg = ParseColor(def.Foreground);
        var bg = ParseColor(def.Background);
        return new Terminal.Gui.Attribute(fg, bg);
    }

    private static Color ParseColor(string colorName)
    {
        // Support hex colors like #FFFF00 or #FF0000
        if (colorName.StartsWith('#'))
        {
            return ParseHexColor(colorName);
        }

        return colorName.ToLowerInvariant() switch
        {
            "black" => Color.Black,
            "blue" => Color.Blue,
            "green" => Color.Green,
            "cyan" => Color.Cyan,
            "red" => Color.Red,
            "magenta" => Color.Magenta,
            "brown" or "yellow" => Color.Yellow,
            "gray" or "grey" => Color.Gray,
            "darkgray" or "darkgrey" => Color.DarkGray,
            "brightblue" => Color.BrightBlue,
            "brightgreen" => Color.BrightGreen,
            "brightcyan" => Color.BrightCyan,
            "brightred" => Color.BrightRed,
            "brightmagenta" => Color.BrightMagenta,
            "brightyellow" => Color.BrightYellow,
            "white" => Color.White,
            _ => Color.White
        };
    }

    private static Color ParseHexColor(string hex)
    {
        try
        {
            // Remove the # prefix
            var colorStr = hex.TrimStart('#');

            // Handle short format (#RGB -> #RRGGBB)
            if (colorStr.Length == 3)
            {
                colorStr = string.Concat(
                    colorStr[0], colorStr[0],
                    colorStr[1], colorStr[1],
                    colorStr[2], colorStr[2]);
            }

            // Handle very short format (#RG -> might mean just two hex digits for a color)
            if (colorStr.Length < 6)
            {
                colorStr = colorStr.PadRight(6, '0');
            }

            var r = Convert.ToByte(colorStr.Substring(0, 2), 16);
            var g = Convert.ToByte(colorStr.Substring(2, 2), 16);
            var b = Convert.ToByte(colorStr.Substring(4, 2), 16);

            return new Color(r, g, b);
        }
        catch
        {
            AppLogger.Logger.Warning("Failed to parse hex color: {Hex}, using White", hex);
            return Color.White;
        }
    }

    /// <summary>
    /// Fallback theme definition when themes.json is missing or broken.
    /// </summary>
    private static ThemeDefinition GetFallbackTheme()
    {
        return new ThemeDefinition
        {
            Name = "Classic Blue",
            Description = "White on Blue - TurboPascal 5 style (Fallback)",
            Default = new ColorSchemeDefinition
            {
                Normal = new ColorAttributeDefinition { Foreground = "White", Background = "Blue" },
                Focus = new ColorAttributeDefinition { Foreground = "Black", Background = "Cyan" },
                HotNormal = new ColorAttributeDefinition { Foreground = "BrightCyan", Background = "Blue" },
                HotFocus = new ColorAttributeDefinition { Foreground = "Black", Background = "BrightCyan" },
                Disabled = new ColorAttributeDefinition { Foreground = "Gray", Background = "Blue" }
            },
            Menu = new ColorSchemeDefinition
            {
                Normal = new ColorAttributeDefinition { Foreground = "White", Background = "DarkGray" },
                Focus = new ColorAttributeDefinition { Foreground = "Black", Background = "Cyan" },
                HotNormal = new ColorAttributeDefinition { Foreground = "BrightYellow", Background = "DarkGray" },
                HotFocus = new ColorAttributeDefinition { Foreground = "BrightYellow", Background = "Cyan" },
                Disabled = new ColorAttributeDefinition { Foreground = "Gray", Background = "DarkGray" }
            },
            Primary = new ColorSchemeDefinition
            {
                Normal = new ColorAttributeDefinition { Foreground = "Cyan", Background = "Blue" },
                Focus = new ColorAttributeDefinition { Foreground = "Black", Background = "Cyan" },
                HotNormal = new ColorAttributeDefinition { Foreground = "BrightCyan", Background = "Blue" },
                HotFocus = new ColorAttributeDefinition { Foreground = "Black", Background = "BrightCyan" },
                Disabled = new ColorAttributeDefinition { Foreground = "Gray", Background = "Blue" }
            },
            Accent = new ColorSchemeDefinition
            {
                Normal = new ColorAttributeDefinition { Foreground = "BrightYellow", Background = "Blue" },
                Focus = new ColorAttributeDefinition { Foreground = "Black", Background = "BrightYellow" },
                HotNormal = new ColorAttributeDefinition { Foreground = "Yellow", Background = "Blue" },
                HotFocus = new ColorAttributeDefinition { Foreground = "Black", Background = "Yellow" },
                Disabled = new ColorAttributeDefinition { Foreground = "Gray", Background = "Blue" }
            },
            Success = new ColorSchemeDefinition
            {
                Normal = new ColorAttributeDefinition { Foreground = "BrightGreen", Background = "Blue" },
                Focus = new ColorAttributeDefinition { Foreground = "Black", Background = "BrightGreen" },
                HotNormal = new ColorAttributeDefinition { Foreground = "Green", Background = "Blue" },
                HotFocus = new ColorAttributeDefinition { Foreground = "Black", Background = "Green" },
                Disabled = new ColorAttributeDefinition { Foreground = "Gray", Background = "Blue" }
            },
            Failure = new ColorSchemeDefinition
            {
                Normal = new ColorAttributeDefinition { Foreground = "BrightRed", Background = "Blue" },
                Focus = new ColorAttributeDefinition { Foreground = "Black", Background = "BrightRed" },
                HotNormal = new ColorAttributeDefinition { Foreground = "Red", Background = "Blue" },
                HotFocus = new ColorAttributeDefinition { Foreground = "Black", Background = "Red" },
                Disabled = new ColorAttributeDefinition { Foreground = "Gray", Background = "Blue" }
            },
            Warning = new ColorSchemeDefinition
            {
                Normal = new ColorAttributeDefinition { Foreground = "Yellow", Background = "Blue" },
                Focus = new ColorAttributeDefinition { Foreground = "Black", Background = "Yellow" },
                HotNormal = new ColorAttributeDefinition { Foreground = "BrightYellow", Background = "Blue" },
                HotFocus = new ColorAttributeDefinition { Foreground = "Black", Background = "BrightYellow" },
                Disabled = new ColorAttributeDefinition { Foreground = "Gray", Background = "Blue" }
            },
            Muted = new ColorSchemeDefinition
            {
                Normal = new ColorAttributeDefinition { Foreground = "Gray", Background = "Blue" },
                Focus = new ColorAttributeDefinition { Foreground = "Black", Background = "Gray" },
                HotNormal = new ColorAttributeDefinition { Foreground = "White", Background = "Blue" },
                HotFocus = new ColorAttributeDefinition { Foreground = "Black", Background = "White" },
                Disabled = new ColorAttributeDefinition { Foreground = "DarkGray", Background = "Blue" }
            },
            Error = new ColorSchemeDefinition
            {
                Normal = new ColorAttributeDefinition { Foreground = "BrightRed", Background = "Blue" },
                Focus = new ColorAttributeDefinition { Foreground = "White", Background = "Red" },
                HotNormal = new ColorAttributeDefinition { Foreground = "Red", Background = "Blue" },
                HotFocus = new ColorAttributeDefinition { Foreground = "White", Background = "BrightRed" },
                Disabled = new ColorAttributeDefinition { Foreground = "Gray", Background = "Blue" }
            }
        };
    }
}
