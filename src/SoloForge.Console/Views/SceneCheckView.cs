using Terminal.Gui;
using SoloForge.Console.App;
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
    private readonly Label _chaosLabel;
    private readonly Button _rollButton;
    private readonly FrameView _resultFrame;
    private readonly Label _resultLabel;
    private readonly Label _detailsLabel;
    private readonly Label _adjustmentLabel;
    private readonly Label _eventLabel;
    private readonly Button _rerollButton;

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
            Height = 1,
            ReadOnly = true,
            CanFocus = true
        };

        var contextButton = new Button
        {
            X = 1,
            Y = 2,
            Text = "Edit Context",
            CanFocus = true
        };
        contextButton.Accepting += (s, e) => EditContext();
        contextButton.KeyDown += (s, e) =>
        {
            if (e == Key.Enter)
            {
                EditContext();
                e.Handled = true;
            }
        };

        // Chaos display (stored as field for updates)
        _chaosLabel = new Label
        {
            X = 1,
            Y = 4,
            Text = $"Current Chaos Factor: {_session.Chaos}"
        };

        // Roll button
        _rollButton = new Button
        {
            X = 1,
            Y = 6,
            Text = "Check Scene (Enter)",
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

        _contextField.KeyDown += (s, e) =>
        {
            if (e == Key.Enter)
            {
                EditContext();
                e.Handled = true;
            }
        };

        // Result display
        _resultFrame = new FrameView
        {
            Title = "Result",
            X = 1,
            Y = 8,
            Width = Dim.Fill(2),
            Height = 14,
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
            Width = Dim.Fill(2),
            Height = 3,
            Text = ""
        };

        _rerollButton = new Button
        {
            X = Pos.Center(),
            Y = 10,
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

        _resultFrame.Add(_resultLabel, _detailsLabel, _adjustmentLabel, _eventLabel, _rerollButton);

        Add(contextLabel, _contextField, contextButton, _chaosLabel, _rollButton, _resultFrame);

        contextButton.FocusDeepest(NavigationDirection.Forward, TabBehavior.TabStop);
    }

    private void EditContext()
    {
        var context = PromptForContext("Describe the current scene setup (optional)");
        _contextField.Text = context ?? string.Empty;
    }

    private string? PromptForContext(string prompt)
    {
        var dialog = new Dialog
        {
            Title = "Scene Context",
            Width = 60,
            Height = 8
        };

        dialog.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc)
            {
                Application.RequestStop();
                e.Handled = true;
            }
        };

        var label = new Label
        {
            X = 1,
            Y = 1,
            Text = prompt
        };

        var field = new TextField
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(2)
        };

        if (!string.IsNullOrWhiteSpace(_contextField.Text.ToString()))
        {
            field.Text = _contextField.Text.ToString();
        }

        string? result = null;

        var okButton = new Button { Text = "OK", IsDefault = true };
        okButton.Accepting += (s, e) =>
        {
            result = field.Text.ToString();
            if (string.IsNullOrWhiteSpace(result)) result = null;
            Application.RequestStop();
        };

        var skipButton = new Button { Text = "Skip" };
        skipButton.Accepting += (s, e) => Application.RequestStop();

        dialog.Add(label, field);
        dialog.AddButton(okButton);
        dialog.AddButton(skipButton);
        field.SetFocus();

        Application.Run(dialog);

        return result;
    }

    private void PerformCheck()
    {
        // Update chaos label to current value
        _chaosLabel.Text = $"Current Chaos Factor: {_session.Chaos}";

        var context = _contextField.Text.ToString();
        if (string.IsNullOrWhiteSpace(context))
        {
            context = null;
        }

        // Perform the scene check
        _lastResult = SceneCheck.PerformCheck(_session.Chaos);

        // Build result details
        var details = $"Roll: {_lastResult.Roll}, Chaos: {_session.Chaos}";
        if (_lastResult.SceneAdjustment != null)
            details += $", Adjustment: {_lastResult.SceneAdjustment}";

        // Log the scene check result
        _historyService.AddEntry(
            LogType.SceneCheck,
            _lastResult.Result,
            context,
            details
        );

        // Append scene check to journal
        var sceneEntry = _historyService.Entries.LastOrDefault();
        if (sceneEntry != null)
        {
            _journalView.AppendEntry(sceneEntry);
        }

        // Log and append random event if triggered
        if (_lastResult.RandomEvent != null)
        {
            _historyService.AddEntry(
                LogType.RandomEvent,
                $"{_lastResult.RandomEvent.EventFocus}: {_lastResult.RandomEvent.EventAction}",
                "Triggered by Scene Interrupt"
            );

            var eventEntry = _historyService.Entries.LastOrDefault();
            if (eventEntry != null)
            {
                _journalView.AppendEntry(eventEntry);
            }
        }

        _campaignService.Save();

        // Display result
        ShowResult();
    }

    private void ShowResult()
    {
        if (_lastResult == null) return;

        var resultText = _lastResult.Result switch
        {
            "Normal Scene" => ">>> Normal Scene <<<",
            "Altered Scene!" => ">>> Altered Scene! <<<",
            "Interrupt Scene!" => ">>> Interrupt Scene! <<<",
            _ => $">>> {_lastResult.Result} <<<"
        };

        _resultLabel.ColorScheme = _lastResult.Result switch
        {
            "Normal Scene" => UiThemes.Instance.ActiveSuccess,
            "Altered Scene!" => UiThemes.Instance.ActiveWarning,
            "Interrupt Scene!" => UiThemes.Instance.ActiveFailure,
            _ => UiThemes.Instance.ActiveDefault
        };
        _resultLabel.Text = resultText;

        _detailsLabel.Text = $"Roll: {_lastResult.Roll}  |  Chaos: {_session.Chaos}";

        if (_lastResult.SceneAdjustment != null)
        {
            _adjustmentLabel.ColorScheme = UiThemes.Instance.ActiveWarning;
            _adjustmentLabel.Text = $"Adjustment: {_lastResult.SceneAdjustment}";
        }
        else
        {
            _adjustmentLabel.Text = "";
        }

        if (_lastResult.RandomEvent != null)
        {
            _eventLabel.ColorScheme = UiThemes.Instance.ActiveAccent;
            _eventLabel.Text = $"RANDOM EVENT: {_lastResult.RandomEvent.EventFocus}\n{_lastResult.RandomEvent.EventAction}";
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
