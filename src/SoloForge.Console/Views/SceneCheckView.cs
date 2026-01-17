using Terminal.Gui;
using SoloForge.Console.Core;
using SoloForge.Console.Engines.Mythic2e;
using SoloForge.Console.Models;
using SoloForge.Console.Services;

namespace SoloForge.Console.Views;

/// <summary>
/// View for performing Scene Checks to determine scene alterations.
/// </summary>
public class SceneCheckView : View
{
    private readonly Session _session;
    private readonly HistoryService _historyService;
    private readonly CampaignService _campaignService;
    private readonly JournalView _journalView;

    private readonly TextField _contextField;
    private readonly FrameView _resultFrame;
    private readonly Label _resultLabel;
    private readonly Label _detailsLabel;
    private readonly Label _adjustmentLabel;
    private readonly Label _eventLabel;

    private SceneCheckResult? _lastResult;

    public SceneCheckView(
        Session session,
        HistoryService historyService,
        CampaignService campaignService,
        JournalView journalView)
    {
        _session = session;
        _historyService = historyService;
        _campaignService = campaignService;
        _journalView = journalView;

        // Context input
        var contextLabel = new Label
        {
            X = 1,
            Y = 0,
            Text = "Scene setup/context (optional):"
        };

        _contextField = new TextField
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Height = 1
        };

        // Chaos display
        var chaosLabel = new Label
        {
            X = 1,
            Y = 3,
            Text = $"Current Chaos Factor: {_session.Chaos}"
        };

        // Roll button
        var rollButton = new Button
        {
            X = 1,
            Y = 5,
            Text = "Check Scene (Enter)",
            IsDefault = true
        };
        rollButton.Accepting += (s, e) => PerformCheck();

        // Result display
        _resultFrame = new FrameView
        {
            Title = "Result",
            X = 1,
            Y = 7,
            Width = Dim.Fill(2),
            Height = 12,
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

        _adjustmentLabel = new Label
        {
            X = 1,
            Y = 4,
            Text = ""
        };

        _eventLabel = new Label
        {
            X = 1,
            Y = 6,
            Text = ""
        };

        _resultFrame.Add(_resultLabel, _detailsLabel, _adjustmentLabel, _eventLabel);

        Add(contextLabel, _contextField, chaosLabel, rollButton, _resultFrame);

        _contextField.SetFocus();
    }

    private void PerformCheck()
    {
        var context = _contextField.Text.ToString();

        // Perform the scene check
        _lastResult = SceneCheck.PerformCheck(_session.Chaos);

        // Build result details
        var details = $"Roll: {_lastResult.Roll}, Chaos: {_session.Chaos}";
        if (_lastResult.SceneAdjustment != null)
            details += $", Adjustment: {_lastResult.SceneAdjustment}";

        // Log the result
        _historyService.AddEntry(
            LogType.SceneCheck,
            _lastResult.Result,
            string.IsNullOrWhiteSpace(context) ? null : context,
            details
        );

        // Log random event if triggered
        if (_lastResult.RandomEvent != null)
        {
            _historyService.AddEntry(
                LogType.RandomEvent,
                $"{_lastResult.RandomEvent.EventFocus}: {_lastResult.RandomEvent.EventAction}",
                "Triggered by Scene Interrupt"
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
        ShowResult();
    }

    private void ShowResult()
    {
        if (_lastResult == null) return;

        var (resultColor, resultText) = _lastResult.Result switch
        {
            "Normal Scene" => (Color.Green, ">>> Normal Scene <<<"),
            "Altered Scene!" => (Color.Yellow, ">>> Altered Scene! <<<"),
            "Interrupt Scene!" => (Color.Red, ">>> Interrupt Scene! <<<"),
            _ => (Color.White, $">>> {_lastResult.Result} <<<")
        };

        _resultLabel.ColorScheme = new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute(resultColor, Color.Black)
        };
        _resultLabel.Text = resultText;

        _detailsLabel.Text = $"Roll: {_lastResult.Roll}  |  Chaos: {_session.Chaos}";

        if (_lastResult.SceneAdjustment != null)
        {
            _adjustmentLabel.ColorScheme = new ColorScheme
            {
                Normal = new Terminal.Gui.Attribute(Color.Yellow, Color.Black)
            };
            _adjustmentLabel.Text = $"Adjustment: {_lastResult.SceneAdjustment}";
        }
        else
        {
            _adjustmentLabel.Text = "";
        }

        if (_lastResult.RandomEvent != null)
        {
            _eventLabel.ColorScheme = new ColorScheme
            {
                Normal = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black)
            };
            _eventLabel.Text = $"RANDOM EVENT: {_lastResult.RandomEvent.EventFocus}\n{_lastResult.RandomEvent.EventAction}";
        }
        else
        {
            _eventLabel.Text = "";
        }

        _resultFrame.Visible = true;
        SetNeedsLayout();
    }
}
