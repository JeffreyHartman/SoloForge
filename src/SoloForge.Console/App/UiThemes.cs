using Terminal.Gui;
using SoloForge.Console.Services;

namespace SoloForge.Console.App;

/// <summary>
/// Proxy to ThemeService for accessing UI color schemes.
/// Provides consistent ColorSchemes across all views based on the active theme.
/// </summary>
public sealed class UiThemes
{
    private static readonly Lazy<UiThemes> _instance = new(() => new UiThemes());
    public static UiThemes Instance => _instance.Value;

    private UiThemes() { }

    // ============================================================
    // Active Theme Properties (delegates to ThemeService)
    // ============================================================

    /// <summary>Active default scheme based on current theme.</summary>
    public ColorScheme ActiveDefault => ThemeService.Instance.Default;

    /// <summary>Active menu scheme based on current theme.</summary>
    public ColorScheme ActiveMenu => ThemeService.Instance.Menu;

    /// <summary>Active primary scheme based on current theme.</summary>
    public ColorScheme ActivePrimary => ThemeService.Instance.Primary;

    /// <summary>Active accent scheme based on current theme.</summary>
    public ColorScheme ActiveAccent => ThemeService.Instance.Accent;

    /// <summary>Active success scheme based on current theme.</summary>
    public ColorScheme ActiveSuccess => ThemeService.Instance.Success;

    /// <summary>Active failure scheme based on current theme.</summary>
    public ColorScheme ActiveFailure => ThemeService.Instance.Failure;

    /// <summary>Active warning scheme based on current theme.</summary>
    public ColorScheme ActiveWarning => ThemeService.Instance.Warning;

    /// <summary>Active muted scheme based on current theme.</summary>
    public ColorScheme ActiveMuted => ThemeService.Instance.Muted;

    /// <summary>Active error scheme based on current theme.</summary>
    public ColorScheme ActiveError => ThemeService.Instance.Error;

    // ============================================================
    // Legacy Aliases (for backward compatibility)
    // ============================================================

    /// <summary>Default scheme - alias for ActiveDefault.</summary>
    public ColorScheme Default => ActiveDefault;

    /// <summary>Primary scheme - alias for ActivePrimary.</summary>
    public ColorScheme Primary => ActivePrimary;

    /// <summary>Accent scheme - alias for ActiveAccent.</summary>
    public ColorScheme Accent => ActiveAccent;

    /// <summary>Success scheme - alias for ActiveSuccess.</summary>
    public ColorScheme Success => ActiveSuccess;

    /// <summary>Failure scheme - alias for ActiveFailure.</summary>
    public ColorScheme Failure => ActiveFailure;

    /// <summary>Warning scheme - alias for ActiveWarning.</summary>
    public ColorScheme Warning => ActiveWarning;

    /// <summary>Error scheme - alias for ActiveError.</summary>
    public ColorScheme Error => ActiveError;

    /// <summary>Muted scheme - alias for ActiveMuted.</summary>
    public ColorScheme Muted => ActiveMuted;

    // ============================================================
    // Helper Methods
    // ============================================================

    /// <summary>
    /// Returns the appropriate ColorScheme for a fate check result.
    /// </summary>
    /// <param name="result">The fate check result string (e.g., "Yes", "Exceptional No").</param>
    /// <returns>Success scheme for Yes results, Failure scheme for No results.</returns>
    public ColorScheme ForFateResult(string? result) => ThemeService.Instance.ForFateResult(result);

    /// <summary>
    /// Returns the appropriate ColorScheme for a scene check result.
    /// </summary>
    /// <param name="result">The scene check result string.</param>
    /// <returns>Success for Normal, Warning for Altered, Failure for Interrupt.</returns>
    public ColorScheme ForSceneResult(string? result) => ThemeService.Instance.ForSceneResult(result);

    /// <summary>
    /// Returns the appropriate ColorScheme for chaos factor value.
    /// </summary>
    /// <param name="chaos">The chaos factor (1-9).</param>
    /// <returns>Success for low (1-3), Warning for mid (4-6), Failure for high (7-9).</returns>
    public ColorScheme ForChaos(int chaos) => ThemeService.Instance.ForChaos(chaos);

    /// <summary>
    /// Returns a ColorScheme for chaos displayed on a header background.
    /// </summary>
    /// <param name="chaos">The chaos factor (1-9).</param>
    /// <returns>Colored text on menu background based on chaos level.</returns>
    public ColorScheme ForChaosOnHeader(int chaos)
    {
        var baseScheme = ForChaos(chaos);
        var menuBg = ThemeService.Instance.Menu.Normal.Background;

        return new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute(baseScheme.Normal.Foreground, menuBg),
            Focus = new Terminal.Gui.Attribute(baseScheme.Focus.Foreground, menuBg),
            HotNormal = new Terminal.Gui.Attribute(baseScheme.HotNormal.Foreground, menuBg),
            HotFocus = new Terminal.Gui.Attribute(baseScheme.HotFocus.Foreground, menuBg),
            Disabled = new Terminal.Gui.Attribute(baseScheme.Disabled.Foreground, menuBg)
        };
    }

    // ============================================================
    // Theme Management
    // ============================================================

    /// <summary>
    /// The name of the currently active theme.
    /// </summary>
    public string ActiveThemeName => ThemeService.Instance.ActiveThemeName;

    /// <summary>
    /// List of available theme names.
    /// </summary>
    public IReadOnlyList<string> AvailableThemes => ThemeService.Instance.AvailableThemes;

    /// <summary>
    /// Applies a theme by name.
    /// </summary>
    /// <param name="themeName">The name of the theme to apply.</param>
    /// <returns>True if the theme was found and applied.</returns>
    public bool ApplyTheme(string themeName) => ThemeService.Instance.ApplyAndSaveTheme(themeName);

    /// <summary>
    /// Event fired when the theme changes.
    /// </summary>
    public event Action? ThemeChanged
    {
        add => ThemeService.Instance.ThemeChanged += value;
        remove => ThemeService.Instance.ThemeChanged -= value;
    }
}
