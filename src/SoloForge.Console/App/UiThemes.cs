using Terminal.Gui;

namespace SoloForge.Console.App;

/// <summary>
/// Centralized theme definitions for Terminal.Gui UI elements.
/// Provides consistent ColorSchemes across all views following the Mythic 2e aesthetic.
/// </summary>
public sealed class UiThemes
{
    private static readonly Lazy<UiThemes> _instance = new(() => new UiThemes());
    public static UiThemes Instance => _instance.Value;

    private UiThemes() { }

    /// <summary>
    /// Default scheme for the application (White on Black, Cyan accents).
    /// </summary>
    public ColorScheme Default { get; } = new ColorScheme
    {
        Normal = new Terminal.Gui.Attribute(Color.White, Color.Black),
        Focus = new Terminal.Gui.Attribute(Color.Black, Color.Cyan),
        HotNormal = new Terminal.Gui.Attribute(Color.Cyan, Color.Black),
        HotFocus = new Terminal.Gui.Attribute(Color.Black, Color.Cyan),
        Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Black)
    };

    /// <summary>
    /// Session info bar header (Black on Cyan).
    /// </summary>
    public ColorScheme SessionHeader { get; } = new ColorScheme
    {
        Normal = new Terminal.Gui.Attribute(Color.Black, Color.Cyan),
        Focus = new Terminal.Gui.Attribute(Color.Black, Color.BrightCyan),
        HotNormal = new Terminal.Gui.Attribute(Color.DarkGray, Color.Cyan),
        HotFocus = new Terminal.Gui.Attribute(Color.DarkGray, Color.BrightCyan),
        Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Cyan)
    };

    /// <summary>
    /// Session header with white values (White on Cyan).
    /// </summary>
    public ColorScheme SessionHeaderValue { get; } = new ColorScheme
    {
        Normal = new Terminal.Gui.Attribute(Color.White, Color.Cyan),
        Focus = new Terminal.Gui.Attribute(Color.White, Color.BrightCyan),
        HotNormal = new Terminal.Gui.Attribute(Color.White, Color.Cyan),
        HotFocus = new Terminal.Gui.Attribute(Color.White, Color.BrightCyan),
        Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Cyan)
    };

    /// <summary>
    /// Primary emphasis for headers and labels (Cyan on Black).
    /// </summary>
    public ColorScheme Primary { get; } = new ColorScheme
    {
        Normal = new Terminal.Gui.Attribute(Color.Cyan, Color.Black),
        Focus = new Terminal.Gui.Attribute(Color.Black, Color.Cyan),
        HotNormal = new Terminal.Gui.Attribute(Color.BrightCyan, Color.Black),
        HotFocus = new Terminal.Gui.Attribute(Color.Black, Color.BrightCyan),
        Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Black)
    };

    /// <summary>
    /// Accent/highlight for results and key information (Gold/BrightYellow on Black).
    /// </summary>
    public ColorScheme Accent { get; } = new ColorScheme
    {
        Normal = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
        Focus = new Terminal.Gui.Attribute(Color.Black, Color.BrightYellow),
        HotNormal = new Terminal.Gui.Attribute(Color.Yellow, Color.Black),
        HotFocus = new Terminal.Gui.Attribute(Color.Black, Color.Yellow),
        Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Black)
    };

    /// <summary>
    /// Success/positive results (Green on Black) - for Yes results, Normal Scene.
    /// </summary>
    public ColorScheme Success { get; } = new ColorScheme
    {
        Normal = new Terminal.Gui.Attribute(Color.Green, Color.Black),
        Focus = new Terminal.Gui.Attribute(Color.Black, Color.Green),
        HotNormal = new Terminal.Gui.Attribute(Color.BrightGreen, Color.Black),
        HotFocus = new Terminal.Gui.Attribute(Color.Black, Color.BrightGreen),
        Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Black)
    };

    /// <summary>
    /// Failure/negative results (Red on Black) - for No results, Interrupt Scene.
    /// </summary>
    public ColorScheme Failure { get; } = new ColorScheme
    {
        Normal = new Terminal.Gui.Attribute(Color.Red, Color.Black),
        Focus = new Terminal.Gui.Attribute(Color.Black, Color.Red),
        HotNormal = new Terminal.Gui.Attribute(Color.BrightRed, Color.Black),
        HotFocus = new Terminal.Gui.Attribute(Color.Black, Color.BrightRed),
        Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Black)
    };

    /// <summary>
    /// Warning/altered state (Yellow on Black) - for Altered Scene, Random Events.
    /// </summary>
    public ColorScheme Warning { get; } = new ColorScheme
    {
        Normal = new Terminal.Gui.Attribute(Color.Yellow, Color.Black),
        Focus = new Terminal.Gui.Attribute(Color.Black, Color.Yellow),
        HotNormal = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black),
        HotFocus = new Terminal.Gui.Attribute(Color.Black, Color.BrightYellow),
        Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Black)
    };

    /// <summary>
    /// Error messages and validation failures (BrightRed on Black).
    /// </summary>
    public ColorScheme Error { get; } = new ColorScheme
    {
        Normal = new Terminal.Gui.Attribute(Color.BrightRed, Color.Black),
        Focus = new Terminal.Gui.Attribute(Color.Black, Color.BrightRed),
        HotNormal = new Terminal.Gui.Attribute(Color.Red, Color.Black),
        HotFocus = new Terminal.Gui.Attribute(Color.Black, Color.Red),
        Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Black)
    };

    /// <summary>
    /// Muted/secondary information (Gray on Black).
    /// </summary>
    public ColorScheme Muted { get; } = new ColorScheme
    {
        Normal = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
        Focus = new Terminal.Gui.Attribute(Color.Black, Color.Gray),
        HotNormal = new Terminal.Gui.Attribute(Color.White, Color.Black),
        HotFocus = new Terminal.Gui.Attribute(Color.Black, Color.White),
        Disabled = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black)
    };

    // ============================================================
    // Classic Blue Theme (90s DOS aesthetic)
    // ============================================================

    /// <summary>
    /// Classic Blue default scheme (White on Blue).
    /// </summary>
    public ColorScheme ClassicBlueDefault { get; } = new ColorScheme
    {
        Normal = new Terminal.Gui.Attribute(Color.White, Color.Blue),
        Focus = new Terminal.Gui.Attribute(Color.Black, Color.Cyan),
        HotNormal = new Terminal.Gui.Attribute(Color.BrightCyan, Color.Blue),
        HotFocus = new Terminal.Gui.Attribute(Color.Black, Color.BrightCyan),
        Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Blue)
    };

    /// <summary>
    /// Classic Blue menu bar scheme (White on DarkGray for contrast).
    /// </summary>
    public ColorScheme ClassicBlueMenu { get; } = new ColorScheme
    {
        Normal = new Terminal.Gui.Attribute(Color.White, Color.DarkGray),
        Focus = new Terminal.Gui.Attribute(Color.Black, Color.Cyan),
        HotNormal = new Terminal.Gui.Attribute(Color.BrightYellow, Color.DarkGray),
        HotFocus = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Cyan),
        Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.DarkGray)
    };

    /// <summary>
    /// Classic Blue success scheme (BrightGreen on Blue).
    /// </summary>
    public ColorScheme ClassicBlueSuccess { get; } = new ColorScheme
    {
        Normal = new Terminal.Gui.Attribute(Color.BrightGreen, Color.Blue),
        Focus = new Terminal.Gui.Attribute(Color.Black, Color.BrightGreen),
        HotNormal = new Terminal.Gui.Attribute(Color.Green, Color.Blue),
        HotFocus = new Terminal.Gui.Attribute(Color.Black, Color.Green),
        Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Blue)
    };

    /// <summary>
    /// Classic Blue failure scheme (BrightRed on Blue).
    /// </summary>
    public ColorScheme ClassicBlueFailure { get; } = new ColorScheme
    {
        Normal = new Terminal.Gui.Attribute(Color.BrightRed, Color.Blue),
        Focus = new Terminal.Gui.Attribute(Color.Black, Color.BrightRed),
        HotNormal = new Terminal.Gui.Attribute(Color.Red, Color.Blue),
        HotFocus = new Terminal.Gui.Attribute(Color.Black, Color.Red),
        Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Blue)
    };

    /// <summary>
    /// Classic Blue accent scheme (BrightYellow/Gold on Blue).
    /// </summary>
    public ColorScheme ClassicBlueAccent { get; } = new ColorScheme
    {
        Normal = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Blue),
        Focus = new Terminal.Gui.Attribute(Color.Black, Color.BrightYellow),
        HotNormal = new Terminal.Gui.Attribute(Color.Yellow, Color.Blue),
        HotFocus = new Terminal.Gui.Attribute(Color.Black, Color.Yellow),
        Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Blue)
    };

    /// <summary>
    /// Classic Blue warning scheme (Yellow on Blue).
    /// </summary>
    public ColorScheme ClassicBlueWarning { get; } = new ColorScheme
    {
        Normal = new Terminal.Gui.Attribute(Color.Yellow, Color.Blue),
        Focus = new Terminal.Gui.Attribute(Color.Black, Color.Yellow),
        HotNormal = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Blue),
        HotFocus = new Terminal.Gui.Attribute(Color.Black, Color.BrightYellow),
        Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Blue)
    };

    /// <summary>
    /// Classic Blue muted scheme (Gray on Blue).
    /// </summary>
    public ColorScheme ClassicBlueMuted { get; } = new ColorScheme
    {
        Normal = new Terminal.Gui.Attribute(Color.Gray, Color.Blue),
        Focus = new Terminal.Gui.Attribute(Color.Black, Color.Gray),
        HotNormal = new Terminal.Gui.Attribute(Color.White, Color.Blue),
        HotFocus = new Terminal.Gui.Attribute(Color.Black, Color.White),
        Disabled = new Terminal.Gui.Attribute(Color.DarkGray, Color.Blue)
    };

    /// <summary>
    /// Classic Blue primary scheme (Cyan on Blue).
    /// </summary>
    public ColorScheme ClassicBluePrimary { get; } = new ColorScheme
    {
        Normal = new Terminal.Gui.Attribute(Color.Cyan, Color.Blue),
        Focus = new Terminal.Gui.Attribute(Color.Black, Color.Cyan),
        HotNormal = new Terminal.Gui.Attribute(Color.BrightCyan, Color.Blue),
        HotFocus = new Terminal.Gui.Attribute(Color.Black, Color.BrightCyan),
        Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Blue)
    };

    // ============================================================
    // Theme Selection
    // ============================================================

    /// <summary>
    /// Whether to use Classic Blue theme (true) or Dark theme (false).
    /// </summary>
    public bool UseClassicBlue { get; set; } = true;

    /// <summary>Active default scheme based on current theme.</summary>
    public ColorScheme ActiveDefault => UseClassicBlue ? ClassicBlueDefault : Default;

    /// <summary>Active success scheme based on current theme.</summary>
    public ColorScheme ActiveSuccess => UseClassicBlue ? ClassicBlueSuccess : Success;

    /// <summary>Active failure scheme based on current theme.</summary>
    public ColorScheme ActiveFailure => UseClassicBlue ? ClassicBlueFailure : Failure;

    /// <summary>Active accent scheme based on current theme.</summary>
    public ColorScheme ActiveAccent => UseClassicBlue ? ClassicBlueAccent : Accent;

    /// <summary>Active warning scheme based on current theme.</summary>
    public ColorScheme ActiveWarning => UseClassicBlue ? ClassicBlueWarning : Warning;

    /// <summary>Active muted scheme based on current theme.</summary>
    public ColorScheme ActiveMuted => UseClassicBlue ? ClassicBlueMuted : Muted;

    /// <summary>Active primary scheme based on current theme.</summary>
    public ColorScheme ActivePrimary => UseClassicBlue ? ClassicBluePrimary : Primary;

    /// <summary>Active menu scheme based on current theme.</summary>
    public ColorScheme ActiveMenu => UseClassicBlue ? ClassicBlueMenu : Default;

    // ============================================================
    // Helper Methods
    // ============================================================

    /// <summary>
    /// Returns the appropriate ColorScheme for a fate check result.
    /// </summary>
    /// <param name="result">The fate check result string (e.g., "Yes", "Exceptional No").</param>
    /// <returns>Success scheme for Yes results, Failure scheme for No results.</returns>
    public ColorScheme ForFateResult(string? result)
    {
        if (string.IsNullOrEmpty(result))
        {
            return Failure;
        }
        return result.Contains("Yes") ? Success : Failure;
    }

    /// <summary>
    /// Returns the appropriate ColorScheme for a scene check result.
    /// </summary>
    /// <param name="result">The scene check result string.</param>
    /// <returns>Success for Normal, Warning for Altered, Failure for Interrupt.</returns>
    public ColorScheme ForSceneResult(string? result) => result switch
    {
        "Normal Scene" => Success,
        "Altered Scene!" => Warning,
        "Interrupt Scene!" => Failure,
        null => Default,
        _ => Default
    };

    /// <summary>
    /// Returns the appropriate ColorScheme for chaos factor value.
    /// </summary>
    /// <param name="chaos">The chaos factor (1-9).</param>
    /// <returns>Success for low (1-3), Warning for mid (4-6), Failure for high (7-9).</returns>
    public ColorScheme ForChaos(int chaos) => chaos switch
    {
        <= 3 => Success,
        <= 6 => Warning,
        _ => Failure
    };

    /// <summary>
    /// Returns a ColorScheme for chaos displayed on cyan background (session header).
    /// </summary>
    /// <param name="chaos">The chaos factor (1-9).</param>
    /// <returns>Colored text on Cyan background based on chaos level.</returns>
    public ColorScheme ForChaosOnHeader(int chaos)
    {
        var color = chaos switch
        {
            <= 3 => Color.Green,
            <= 6 => Color.Yellow,
            _ => Color.Red
        };
        return new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute(color, Color.Cyan),
            Focus = new Terminal.Gui.Attribute(color, Color.BrightCyan),
            HotNormal = new Terminal.Gui.Attribute(color, Color.Cyan),
            HotFocus = new Terminal.Gui.Attribute(color, Color.BrightCyan),
            Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Cyan)
        };
    }
}
