using Serilog;
using SoloForge.Console.Models;

namespace SoloForge.Console.Services;

public sealed class JournalService
{
    private readonly Func<Guid, string> _journalPathResolver;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ILogger _log = AppLogger.ForContext<JournalService>();

    public JournalService(Func<Guid, string> journalPathResolver, ITemplateRenderer templateRenderer)
    {
        _journalPathResolver = journalPathResolver ?? throw new ArgumentNullException(nameof(journalPathResolver));
        _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
    }

    public string LoadOrCreate(Guid campaignId, string campaignName)
    {
        var path = _journalPathResolver(campaignId);

        try
        {
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to read journal: {Path}", path);
        }

        return JournalDefaults.CreateDefault(campaignName);
    }

    public bool Save(Guid campaignId, string content)
    {
        var path = _journalPathResolver(campaignId);

        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(path, content ?? string.Empty);
            return true;
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to save journal: {Path}", path);
            return false;
        }
    }

    public string ToMarkdown(LogEntry entry) => _templateRenderer.ToMarkdown(entry);

    public string AppendEntryToText(string currentText, LogEntry entry)
    {
        var markdown = ToMarkdown(entry);
        return AppendMarkdownToText(currentText, markdown);
    }

    public string AppendMarkdownToText(string currentText, string markdown)
    {
        return JournalTextComposer.AppendMarkdown(currentText, markdown);
    }
}
