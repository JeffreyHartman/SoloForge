using Terminal.Gui;
using SoloForge.Console.App;
using SoloForge.Console.Models;
using SoloForge.Console.Services;

namespace SoloForge.Console.Views.Components;

public sealed class JournalPanel : FrameView
{
    private readonly CampaignService _campaignService;
    private readonly JournalService _journalService;

    private readonly CheckBox _eventsCheckBox;
    private readonly CheckBox _diceRollsCheckBox;
    private readonly TextView _textView;

    private System.Timers.Timer? _saveTimer;
    private bool _isDirty;
    private bool _suppressChangeTracking;
    private readonly object _saveLock = new();

    private Guid? _loadedCampaignId;

    public JournalPanel(CampaignService campaignService, JournalService journalService)
    {
        _campaignService = campaignService;
        _journalService = journalService;

        Title = "Journal";
        BorderStyle = LineStyle.Double;
        CanFocus = true;
        Visible = false;

        _eventsCheckBox = new CheckBox
        {
            X = 0,
            Y = 0,
            Text = "Auto-log events",
            CheckedState = CheckState.Checked
        };

        _diceRollsCheckBox = new CheckBox
        {
            X = Pos.Right(_eventsCheckBox) + 2,
            Y = 0,
            Text = "Auto-log dice rolls",
            CheckedState = CheckState.Checked
        };

        _eventsCheckBox.CheckedStateChanging += OnEventsToggleChanged;
        _diceRollsCheckBox.CheckedStateChanging += OnDiceRollsToggleChanged;

        _textView = new TextView
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = false,
            WordWrap = true,
            CanFocus = true
        };

        Add(_eventsCheckBox, _diceRollsCheckBox, _textView);

        ApplyTheme();

        _textView.ContentsChanged += OnTextChanged;
        _textView.HasFocusChanged += (s, e) =>
        {
            if (!_textView.HasFocus)
            {
                FlushSave();
            }
        };

        _saveTimer = new System.Timers.Timer(500);
        _saveTimer.AutoReset = false;
        _saveTimer.Elapsed += (s, e) =>
        {
            Application.Invoke(() => FlushSave());
        };

        ReloadForCampaign();
    }

    public void ApplyTheme()
    {
        ColorScheme = UiThemes.Instance.ActiveDefault;
        _eventsCheckBox.ColorScheme = UiThemes.Instance.ActiveDefault;
        _diceRollsCheckBox.ColorScheme = UiThemes.Instance.ActiveDefault;
        _textView.ColorScheme = UiThemes.Instance.ActiveDefault;
        SetNeedsLayout();
    }

    public void ReloadForCampaign()
    {
        FlushSave();

        var campaign = _campaignService.CurrentCampaign;
        if (campaign == null)
        {
            _loadedCampaignId = null;
            return;
        }

        LoadCampaignJournal(campaign);
    }

    public void AppendEntry(LogEntry entry)
    {
        var campaign = _campaignService.CurrentCampaign;
        if (campaign == null)
        {
            return;
        }

        if (entry.Type == LogType.DiceRoll && !campaign.AutoJournalDiceRolls)
        {
            return;
        }

        if (entry.Type != LogType.DiceRoll && entry.Type != LogType.Note && !campaign.AutoJournalEvents)
        {
            return;
        }

        AppendMarkdown(_journalService.ToMarkdown(entry));
    }

    public void AppendMarkdown(string markdown)
    {
        var campaign = _campaignService.CurrentCampaign;
        if (campaign == null)
        {
            return;
        }

        if (_loadedCampaignId != campaign.Id)
        {
            ReloadForCampaign();
        }

        var currentText = _textView.Text.ToString() ?? string.Empty;
        var updated = _journalService.AppendMarkdownToText(currentText, markdown);

        _suppressChangeTracking = true;
        try
        {
            _textView.Text = updated;
            _textView.MoveEnd();
        }
        finally
        {
            _suppressChangeTracking = false;
        }

        _isDirty = true;
        FlushSave();
    }

    private void LoadCampaignJournal(CampaignData campaign)
    {
        _loadedCampaignId = campaign.Id;

        _suppressChangeTracking = true;
        try
        {
            _eventsCheckBox.CheckedState = campaign.AutoJournalEvents ? CheckState.Checked : CheckState.UnChecked;
            _diceRollsCheckBox.CheckedState = campaign.AutoJournalDiceRolls ? CheckState.Checked : CheckState.UnChecked;
        }
        finally
        {
            _suppressChangeTracking = false;
        }

        var content = _journalService.LoadOrCreate(campaign.Id, campaign.Name);

        _suppressChangeTracking = true;
        try
        {
            _textView.Text = content;
            _textView.MoveEnd();
            _isDirty = false;
        }
        finally
        {
            _suppressChangeTracking = false;
        }

        ApplyTheme();
    }

    private void OnEventsToggleChanged(object? sender, CancelEventArgs<CheckState> e)
    {
        var campaign = _campaignService.CurrentCampaign;
        if (campaign == null)
        {
            return;
        }

        campaign.AutoJournalEvents = e.NewValue == CheckState.Checked;
        _campaignService.Save();
    }

    private void OnDiceRollsToggleChanged(object? sender, CancelEventArgs<CheckState> e)
    {
        var campaign = _campaignService.CurrentCampaign;
        if (campaign == null)
        {
            return;
        }

        campaign.AutoJournalDiceRolls = e.NewValue == CheckState.Checked;
        _campaignService.Save();
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (_suppressChangeTracking)
        {
            return;
        }

        _isDirty = true;
        _saveTimer?.Stop();
        _saveTimer?.Start();
    }

    private void FlushSave()
    {
        lock (_saveLock)
        {
            if (!_isDirty)
            {
                return;
            }

            if (_loadedCampaignId == null)
            {
                return;
            }

            var content = _textView.Text.ToString() ?? string.Empty;
            if (_journalService.Save(_loadedCampaignId.Value, content))
            {
                _isDirty = false;
            }
        }
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
