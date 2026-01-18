using System.Text.Json.Serialization;

namespace SoloForge.Console.Models;

/// <summary>
/// Root container for themes loaded from themes.json.
/// </summary>
public record ThemeCollection
{
    [JsonPropertyName("themes")]
    public List<ThemeDefinition> Themes { get; init; } = [];
}

/// <summary>
/// A complete theme definition with all color schemes.
/// </summary>
public record ThemeDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "Unnamed";

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("default")]
    public ColorSchemeDefinition Default { get; init; } = new();

    [JsonPropertyName("menu")]
    public ColorSchemeDefinition Menu { get; init; } = new();

    [JsonPropertyName("primary")]
    public ColorSchemeDefinition Primary { get; init; } = new();

    [JsonPropertyName("accent")]
    public ColorSchemeDefinition Accent { get; init; } = new();

    [JsonPropertyName("success")]
    public ColorSchemeDefinition Success { get; init; } = new();

    [JsonPropertyName("failure")]
    public ColorSchemeDefinition Failure { get; init; } = new();

    [JsonPropertyName("warning")]
    public ColorSchemeDefinition Warning { get; init; } = new();

    [JsonPropertyName("muted")]
    public ColorSchemeDefinition Muted { get; init; } = new();

    [JsonPropertyName("error")]
    public ColorSchemeDefinition Error { get; init; } = new();
}

/// <summary>
/// Color scheme definition with all attribute states.
/// </summary>
public record ColorSchemeDefinition
{
    [JsonPropertyName("normal")]
    public ColorAttributeDefinition Normal { get; init; } = new();

    [JsonPropertyName("focus")]
    public ColorAttributeDefinition Focus { get; init; } = new();

    [JsonPropertyName("hotNormal")]
    public ColorAttributeDefinition HotNormal { get; init; } = new();

    [JsonPropertyName("hotFocus")]
    public ColorAttributeDefinition HotFocus { get; init; } = new();

    [JsonPropertyName("disabled")]
    public ColorAttributeDefinition Disabled { get; init; } = new();
}

/// <summary>
/// A single color attribute with foreground and background.
/// </summary>
public record ColorAttributeDefinition
{
    [JsonPropertyName("foreground")]
    public string Foreground { get; init; } = "White";

    [JsonPropertyName("background")]
    public string Background { get; init; } = "Black";
}
