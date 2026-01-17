using System.Collections.ObjectModel;
using Terminal.Gui;
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
    private readonly FrameView _resultFrame;
    private readonly Label _resultLabel;
    private readonly Label _detailsLabel;
    private readonly Label _eventLabel;

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
            Height = 1
        };

        // Odds selection
        var oddsLabel = new Label
        {
            X = 1,
            Y = 3,
            Text = "Select Odds:"
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

        // Roll button
        var rollButton = new Button
        {
            X = 1,
            Y = 14,
            Text = "Roll (Enter)",
            IsDefault = true
        };
        rollButton.Accepting += (s, e) => PerformCheck();

        // Result display
        _resultFrame = new FrameView
        {
            Title = "Result",
            X = 1,
            Y = 16,
            Width = Dim.Fill(2),
            Height = 8,
            Visible = false
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
            Text = ""
        };

        _resultFrame.Add(_resultLabel, _detailsLabel, _eventLabel);

        Add(questionLabel, _questionField, oddsLabel, _oddsList, rollButton, _resultFrame);

        _questionField.SetFocus();
    }

    private void OnOddsSelected(object? sender, ListViewItemEventArgs e)
    {
        PerformCheck();
    }

    private void PerformCheck()
    {
        var oddsValues = Enum.GetValues<Odds>();
        var selectedOdds = oddsValues[_oddsList.SelectedItem];
        var question = _questionField.Text.ToString();

        // Perform the fate check
        _lastResult = FateCheck.PerformCheck(_session.Chaos, selectedOdds);

        // Log the result
        _historyService.AddEntry(
            LogType.FateCheck,
            _lastResult.Result,
            string.IsNullOrWhiteSpace(question) ? null : question,
            $"Odds: {selectedOdds.GetDisplayName()}, Roll: {_lastResult.Roll}, Chaos: {_session.Chaos}"
        );

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
        }

        _campaignService.Save();

        // Update journal
        var entry = _historyService.Entries.LastOrDefault();
        if (entry != null)
        {
            _journalView.AppendEntry(entry);
        }

        // Display result
        ShowResult(selectedOdds);
    }

    private void ShowResult(Odds odds)
    {
        if (_lastResult == null) return;

        var resultColor = _lastResult.Result.Contains("Yes") ? Color.Green : Color.Red;
        _resultLabel.ColorScheme = new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute(resultColor, Color.Black)
        };
        _resultLabel.Text = $">>> {_lastResult.Result} <<<";

        _detailsLabel.Text = $"Odds: {odds.GetDisplayName()}  |  Roll: {_lastResult.Roll}  |  Chaos: {_session.Chaos}";

        if (_lastEvent != null)
        {
            _eventLabel.ColorScheme = new ColorScheme
            {
                Normal = new Terminal.Gui.Attribute(Color.Yellow, Color.Black)
            };
            _eventLabel.Text = $"RANDOM EVENT: {_lastEvent.EventFocus}\n{_lastEvent.EventAction}";
        }
        else
        {
            _eventLabel.Text = "";
        }

        _resultFrame.Visible = true;
        SetNeedsLayout();
    }
}
