using System.Collections.ObjectModel;
using Terminal.Gui;
using SoloForge.Console.App;
using SoloForge.Console.Core;
using SoloForge.Console.Engines.Mythic2e;
using SoloForge.Console.Models;
using SoloForge.Console.Services;

namespace SoloForge.Console.Views;

/// <summary>
/// View for performing Fate Checks against the Mythic 2e fate chart.
/// </summary>
public class FateCheckView : View
{
    private readonly Session _session;
    private readonly HistoryService _historyService;
    private readonly CampaignService _campaignService;
    private readonly JournalView _journalView;

    private readonly TextField _questionField;
    private readonly ListView _oddsList;
    private readonly Button _rollButton;
    private readonly FrameView _resultFrame;
    private readonly Label _resultLabel;
    private readonly Label _detailsLabel;
    private readonly Label _eventLabel;
    private readonly Button _rerollButton;

    private FateCheckResult? _lastResult;
    private RandomEventResult? _lastEvent;

    public FateCheckView(
        Session session,
        HistoryService historyService,
        CampaignService campaignService,
        JournalView journalView)
    {
        _session = session;
        _historyService = historyService;
        _campaignService = campaignService;
        _journalView = journalView;

        // Question input
        var questionLabel = new Label
        {
            X = 1,
            Y = 0,
            Text = "Question (optional):"
        };

        _questionField = new TextField
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = 1,
            CanFocus = true
        };

        // Odds selection
        var oddsLabel = new Label
        {
            X = 1,
            Y = 3,
            Text = "Select Odds (use arrows, Enter to roll):"
        };

        var oddsValues = Enum.GetValues<Odds>();
        var oddsNames = new ObservableCollection<string>(oddsValues.Select(o => o.GetDisplayName()));

        _oddsList = new ListView
        {
            X = 1,
            Y = 4,
            Width = Dim.Fill(2),
            Height = 9
        };
        _oddsList.SetSource(oddsNames);
        _oddsList.SelectedItem = 4; // Default to "50/50"
        _oddsList.OpenSelectedItem += OnOddsSelected;
        _oddsList.KeyDown += OnOddsKeyDown;
        _oddsList.TabStop = TabBehavior.TabStop;
        _oddsList.CanFocus = true;

        // Roll button
        _rollButton = new Button
        {
            X = 1,
            Y = 14,
            Text = "Roll (Enter)",
            IsDefault = true,
            CanFocus = true
        };
        _rollButton.Accepting += (s, e) => PerformCheck();
        _rollButton.KeyDown += (s, e) =>
        {
            if (e == Key.Enter)
            {
                PerformCheck();
                e.Handled = true;
            }
        };

        // Result display
        _resultFrame = new FrameView
        {
            Title = "Result",
            X = 1,
            Y = 16,
            Width = Dim.Fill(2),
            Height = 10,
            Visible = false,
            CanFocus = true
        };

        _resultLabel = new Label
        {
            X = Pos.Center(),
            Y = 0,
            Text = ""
        };

        _detailsLabel = new Label
        {
            X = 1,
            Y = 2,
            Text = ""
        };

        _eventLabel = new Label
        {
            X = 1,
            Y = 4,
            Width = Dim.Fill(2),
            Height = 2,
            Text = ""
        };

        _rerollButton = new Button
        {
            X = Pos.Center(),
            Y = 7,
            Text = "Re-roll (Enter)",
            IsDefault = false,
            CanFocus = true
        };
        _rerollButton.Accepting += (s, e) => PerformCheck();
        _rerollButton.KeyDown += (s, e) =>
        {
            if (e == Key.Enter)
            {
                PerformCheck();
                e.Handled = true;
            }
        };

        _resultFrame.Add(_resultLabel, _detailsLabel, _eventLabel, _rerollButton);

        Add(questionLabel, _questionField, oddsLabel, _oddsList, _rollButton, _resultFrame);

        _oddsList.FocusDeepest(NavigationDirection.Forward, TabBehavior.TabStop);
    }

    private void OnOddsSelected(object? sender, ListViewItemEventArgs e)
    {
        PerformCheck();
    }

    private void OnOddsKeyDown(object? sender, Key e)
    {
        if (e == Key.Enter)
        {
            PerformCheck();
            e.Handled = true;
        }
    }

    private void PerformCheck()
    {
        var oddsValues = Enum.GetValues<Odds>();
        var selectedOdds = oddsValues[_oddsList.SelectedItem];
        var question = _questionField.Text.ToString();

        // Perform the fate check
        _lastResult = FateCheck.PerformCheck(_session.Chaos, selectedOdds);

        // Log the fate check result
        _historyService.AddEntry(
            LogType.FateCheck,
            _lastResult.Result,
            string.IsNullOrWhiteSpace(question) ? null : question,
            $"Odds: {selectedOdds.GetDisplayName()}, Roll: {_lastResult.Roll}, Chaos: {_session.Chaos}"
        );

        // Append fate check to journal
        var fateEntry = _historyService.Entries.LastOrDefault();
        if (fateEntry != null)
        {
            _journalView.AppendEntry(fateEntry);
        }

        // Check for random event
        _lastEvent = null;
        if (_lastResult.RandomEventTriggered)
        {
            _lastEvent = RandomEvent.Generate();
            _historyService.AddEntry(
                LogType.RandomEvent,
                $"{_lastEvent.EventFocus}: {_lastEvent.EventAction}",
                "Triggered by Fate Check"
            );

            // Append random event to journal
            var eventEntry = _historyService.Entries.LastOrDefault();
            if (eventEntry != null)
            {
                _journalView.AppendEntry(eventEntry);
            }
        }

        _campaignService.Save();

        // Display result
        ShowResult(selectedOdds);
    }

    private void ShowResult(Odds odds)
    {
        if (_lastResult == null) return;

        _resultLabel.ColorScheme = _lastResult.Result.Contains("Yes")
            ? UiThemes.Instance.ActiveSuccess : UiThemes.Instance.ActiveFailure;
        _resultLabel.Text = $">>> {_lastResult.Result} <<<";

        _detailsLabel.Text = $"Odds: {odds.GetDisplayName()}  |  Roll: {_lastResult.Roll}  |  Chaos: {_session.Chaos}";

        if (_lastEvent != null)
        {
            _eventLabel.ColorScheme = UiThemes.Instance.ActiveWarning;
            _eventLabel.Text = $"RANDOM EVENT: {_lastEvent.EventFocus}\n{_lastEvent.EventAction}";
        }
        else
        {
            _eventLabel.Text = "";
        }

        _resultFrame.Visible = true;

        // Switch default button to re-roll
        _rollButton.IsDefault = false;
        _rerollButton.IsDefault = true;
        _rerollButton.FocusDeepest(NavigationDirection.Forward, TabBehavior.TabStop);

        SetNeedsLayout();
    }
}
