using Terminal.Gui;
using SoloForge.Console.Models;
using SoloForge.Console.Services;

namespace SoloForge.Console.Views;

/// <summary>
/// Journal panel using Terminal.Gui's TextView for proper scrolling and text editing.
/// Supports vim-like navigation in normal mode.
/// </summary>
public class JournalView : View
{
    private readonly HistoryService _historyService;
    private readonly CampaignService _campaignService;
    private readonly TextView _textView;

    public JournalView(HistoryService historyService, CampaignService campaignService)
    {
        _historyService = historyService;
        _campaignService = campaignService;

        _textView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = false,
            WordWrap = true,
            ColorScheme = new ColorScheme
            {
                Normal = new Terminal.Gui.Attribute(Color.White, Color.Black),
                Focus = new Terminal.Gui.Attribute(Color.White, Color.DarkGray),
                HotNormal = new Terminal.Gui.Attribute(Color.Cyan, Color.Black),
                HotFocus = new Terminal.Gui.Attribute(Color.Cyan, Color.DarkGray)
            }
        };

        // Load journal content when campaign is available
        LoadJournal();

        // Save on text change
        _textView.ContentsChanged += OnTextChanged;

        Add(_textView);
    }

    private void LoadJournal()
    {
        var campaign = _campaignService.CurrentCampaign;
        if (campaign == null) return;

        var filePath = _campaignService.GetJournalPath(campaign.Id);

        if (File.Exists(filePath))
        {
            _textView.Text = File.ReadAllText(filePath);
            // Move cursor to end
            _textView.MoveEnd();
        }
        else
        {
            _textView.Text = $"# {campaign.Name}\n\nJournal entries will appear here.\n\n";
        }
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        SaveJournal();
    }

    private void SaveJournal()
    {
        var campaign = _campaignService.CurrentCampaign;
        if (campaign == null) return;

        var filePath = _campaignService.GetJournalPath(campaign.Id);

        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(filePath, _textView.Text.ToString() ?? string.Empty);
        }
        catch (Exception ex)
        {
            AppLogger.Logger.Error(ex, "Failed to save journal: {Path}", filePath);
        }
    }

    /// <summary>
    /// Appends a log entry to the journal as markdown.
    /// </summary>
    public void AppendEntry(LogEntry entry)
    {
        var markdown = TemplateService.Instance.ToMarkdown(entry);
        AppendMarkdown(markdown);
    }

    /// <summary>
    /// Appends markdown text to the journal.
    /// </summary>
    public void AppendMarkdown(string markdown)
    {
        var currentText = _textView.Text.ToString() ?? string.Empty;

        // Add blank line before if not empty
        if (!string.IsNullOrWhiteSpace(currentText) && !currentText.EndsWith("\n\n"))
        {
            currentText = currentText.TrimEnd() + "\n\n";
        }

        _textView.Text = currentText + markdown + "\n";
        _textView.MoveEnd();
        SaveJournal();
    }

    /// <summary>
    /// Refreshes the journal from the current campaign.
    /// </summary>
    public void Refresh()
    {
        LoadJournal();
        SetNeedsLayout();
    }

    /// <summary>
    /// Reloads the journal for a new campaign.
    /// </summary>
    public void ReloadForCampaign()
    {
        LoadJournal();
    }
}
