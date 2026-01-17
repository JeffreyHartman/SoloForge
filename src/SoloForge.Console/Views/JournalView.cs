using Terminal.Gui;
using SoloForge.Console.Models;
using SoloForge.Console.Services;

namespace SoloForge.Console.Views;

/// <summary>
/// Journal panel using Terminal.Gui's TextView for proper scrolling and text editing.
/// Implements debounced saving to avoid excessive disk writes.
/// </summary>
public class JournalView : View
{
    private readonly HistoryService _historyService;
    private readonly CampaignService _campaignService;
    private readonly TextView _textView;

    private System.Timers.Timer? _saveTimer;
    private bool _isDirty;
    private readonly object _saveLock = new();

    public JournalView(HistoryService historyService, CampaignService campaignService)
    {
        _historyService = historyService;
        _campaignService = campaignService;

        CanFocus = true;

        _textView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = false,
            WordWrap = true,
            CanFocus = true,
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

        // Debounced save on text change
        _textView.ContentsChanged += OnTextChanged;

        // Save when view loses focus
        HasFocusChanged += (s, e) =>
        {
            if (!HasFocus) FlushSave();
        };

        Add(_textView);

        // Initialize debounce timer (500ms delay)
        _saveTimer = new System.Timers.Timer(500);
        _saveTimer.AutoReset = false;
        _saveTimer.Elapsed += (s, e) =>
        {
            Application.Invoke(() => FlushSave());
        };
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

        _isDirty = false;
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        _isDirty = true;
        // Reset and restart the debounce timer
        _saveTimer?.Stop();
        _saveTimer?.Start();
    }

    private void FlushSave()
    {
        lock (_saveLock)
        {
            if (!_isDirty) return;

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
                _isDirty = false;
            }
            catch (Exception ex)
            {
                AppLogger.Logger.Error(ex, "Failed to save journal: {Path}", filePath);
            }
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

        // Immediate save for appended entries
        _isDirty = true;
        FlushSave();
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
        // Save current before switching
        FlushSave();
        LoadJournal();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FlushSave();
            _saveTimer?.Dispose();
            _saveTimer = null;
        }
        base.Dispose(disposing);
    }
}
