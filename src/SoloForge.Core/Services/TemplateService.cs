using System.Text.RegularExpressions;
using Serilog;
using SoloForge.Core.Models;

namespace SoloForge.Core.Services;

/// <summary>
/// Service for loading and processing markdown templates for log entries.
/// Uses singleton pattern for global access.
/// </summary>
public sealed partial class TemplateService
{
    private static readonly Lazy<TemplateService> _instance = new(() => new TemplateService());
    public static TemplateService Instance => _instance.Value;

    private readonly Dictionary<LogType, string> _templates = new();
    private readonly string _templatesPath;
    private readonly ILogger _log = AppLogger.ForContext<TemplateService>();
    private bool _initialized;

    private TemplateService()
    {
        _templatesPath = FindTemplatesDirectory();
        _log.Debug("TemplateService initialized with templates path: {Path}", _templatesPath);
    }

    /// <summary>
    /// Converts a log entry to formatted markdown using the appropriate template.
    /// </summary>
    public string ToMarkdown(LogEntry entry)
    {
        _log.Debug("Converting {Type} entry to markdown", entry.Type);
        EnsureInitialized();

        var template = GetTemplate(entry.Type);
        var result = PopulateTemplate(template, entry);
        _log.Debug("Generated markdown ({Length} chars) for {Type}", result.Length, entry.Type);
        return result;
    }

    /// <summary>
    /// Gets the template for a specific log type.
    /// </summary>
    public string GetTemplate(LogType type)
    {
        EnsureInitialized();
        return _templates.TryGetValue(type, out var template) ? template : GetDefaultTemplate(type);
    }

    /// <summary>
    /// Reloads all templates from disk.
    /// </summary>
    public void ReloadTemplates()
    {
        _templates.Clear();
        LoadAllTemplates();
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        LoadAllTemplates();
        _initialized = true;
    }

    private void LoadAllTemplates()
    {
        _log.Information("Loading templates from {Path}", _templatesPath);
        foreach (var logType in Enum.GetValues<LogType>())
        {
            var template = LoadTemplateFile(logType);
            if (template != null)
            {
                _templates[logType] = template;
                _log.Debug("Loaded custom template for {Type}", logType);
            }
            else
            {
                _templates[logType] = GetDefaultTemplate(logType);
                _log.Debug("Using default template for {Type}", logType);
            }
        }
        _log.Information("Loaded {Count} templates", _templates.Count);
    }

    private string? LoadTemplateFile(LogType type)
    {
        if (string.IsNullOrEmpty(_templatesPath))
        {
            _log.Debug("No templates path configured");
            return null;
        }

        var fileName = $"{type.ToString().ToLowerInvariant()}.md";
        var filePath = Path.Combine(_templatesPath, fileName);

        // Also try .txt extension
        if (!File.Exists(filePath))
        {
            fileName = $"{type.ToString().ToLowerInvariant()}.txt";
            filePath = Path.Combine(_templatesPath, fileName);
        }

        if (!File.Exists(filePath))
        {
            _log.Debug("Template file not found for {Type}: {Path}", type, filePath);
            return null;
        }

        try
        {
            var content = File.ReadAllText(filePath);
            _log.Debug("Loaded template file: {Path}", filePath);
            return StripComments(content);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to read template file: {Path}", filePath);
            return null;
        }
    }

    private static string StripComments(string content)
    {
        var lines = content.Split('\n');
        var result = new List<string>();
        var inHtmlComment = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Handle HTML comment blocks <!-- ... -->
            if (trimmed.StartsWith("<!--"))
            {
                inHtmlComment = true;
                if (trimmed.Contains("-->"))
                    inHtmlComment = false;
                continue;
            }

            if (inHtmlComment)
            {
                if (trimmed.Contains("-->"))
                    inHtmlComment = false;
                continue;
            }

            // Skip lines starting with ;; (template doc comments)
            if (trimmed.StartsWith(";;"))
                continue;

            result.Add(line);
        }

        return string.Join('\n', result).Trim();
    }

    private static string PopulateTemplate(string template, LogEntry entry)
    {
        var result = template;

        // Replace all placeholders
        result = result.Replace("{Type}", entry.Type.ToString());
        result = result.Replace("{Result}", entry.Result);
        result = result.Replace("{Context}", entry.Context ?? "");
        // Convert newlines to <br> for markdown compatibility in Details
        var markdownDetails = (entry.Details ?? "").Replace("\n", "<br>");
        result = result.Replace("{Details}", markdownDetails);
        result = result.Replace("{Timestamp}", entry.Timestamp.ToString("yyyy-MM-dd h:mm tt"));
        result = result.Replace("{Date}", entry.Timestamp.ToString("yyyy-MM-dd"));
        result = result.Replace("{Time}", entry.Timestamp.ToString("h:mm tt"));
        result = result.Replace("{Id}", entry.Id.ToString());

        // Handle conditional sections: {?Context}...{/Context}
        result = ProcessConditionalSection(result, "Context", entry.Context);
        result = ProcessConditionalSection(result, "Details", entry.Details);

        // Clean up any empty lines at the start/end
        result = result.Trim();

        return result;
    }

    private static string ProcessConditionalSection(string template, string field, string? value)
    {
        // Pattern matches: {?FieldName}content{/FieldName}
        // In regex: \{ and \} match literal braces, \? matches literal question mark
        // In C# verbatim interpolated string: {{ and }} produce literal braces
        var pattern = $@"\{{\?{field}\}}(.*?)\{{/{field}\}}";
        var regex = new Regex(pattern, RegexOptions.Singleline);

        if (string.IsNullOrEmpty(value))
        {
            // Remove the entire conditional section
            return regex.Replace(template, "");
        }

        // Keep the content, remove the markers
        return regex.Replace(template, "$1");
    }

    private static string GetDefaultTemplate(LogType type) => type switch
    {
        LogType.FateCheck => DefaultFateCheckTemplate,
        LogType.SceneCheck => DefaultSceneCheckTemplate,
        LogType.RandomEvent => DefaultRandomEventTemplate,
        LogType.Meaning => DefaultMeaningTemplate,
        LogType.DiceRoll => DefaultDiceRollTemplate,
        LogType.Note => DefaultNoteTemplate,
        _ => DefaultGenericTemplate
    };

    private static string FindTemplatesDirectory()
    {
        // Look for templates directory relative to executable
        var baseDir = AppContext.BaseDirectory;
        var currentDir = new DirectoryInfo(baseDir);

        while (currentDir != null)
        {
            var templatesPath = Path.Combine(currentDir.FullName, "templates");
            if (Directory.Exists(templatesPath))
                return templatesPath;

            currentDir = currentDir.Parent;
        }

        // Return default path even if it doesn't exist
        return Path.Combine(baseDir, "templates");
    }

    #region Default Templates

    private const string DefaultFateCheckTemplate = """
        | Fate Check | &nbsp; |
        | ---------- | ------ |
        | **Question** | {Context} |
        | **Result** | {Result} |
        {?Details}| *Details* | {Details} |{/Details}
        """;

    private const string DefaultSceneCheckTemplate = """
        | Scene Check | &nbsp; |
        | ----------- | ------ |
        {?Context}| **Context** | {Context} |{/Context}
        | **Result** | {Result} |
        {?Details}| *Details* | {Details} |{/Details}
        """;

    private const string DefaultRandomEventTemplate = """
        | Random Event | &nbsp; |
        | ------------ | ------ |
        | **Event** | {Result} |
        {?Details}| *Details* | {Details} |{/Details}
        """;

    private const string DefaultMeaningTemplate = """
        | Meaning Roll | &nbsp; |
        | ------------ | ------ |
        {?Context}| **For** | {Context} |{/Context}
        | **Result** | {Result} |
        {?Details}| *Details* | {Details} |{/Details}
        """;

    private const string DefaultDiceRollTemplate = """
        | Dice Roll | &nbsp; |
        | --------- | ------ |
        | **Expression** | {Context} |
        | **Total** | {Result} |
        {?Details}| *Details* | {Details} |{/Details}
        """;

    private const string DefaultNoteTemplate = """
        > **Note:** {Result}
        """;

    private const string DefaultGenericTemplate = """
        | {Type} | &nbsp; |
        | ------ | ------ |
        {?Context}| **Context** | {Context} |{/Context}
        | **Result** | {Result} |
        {?Details}| *Details* | {Details} |{/Details}
        """;

    #endregion
}
