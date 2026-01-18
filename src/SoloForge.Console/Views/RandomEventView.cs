using Terminal.Gui;
using SoloForge.Console.App;
using SoloForge.Console.Core;
using SoloForge.Console.Engines.Mythic2e;
using SoloForge.Console.Models;
using SoloForge.Console.Services;
using SoloForge.Console.Views.Components;

namespace SoloForge.Console.Views;

/// <summary>
/// View for generating random events with focus and action.
/// </summary>
public class RandomEventView : View
{
    private readonly Session _session;
    private readonly AdventureStateManager _stateManager;
    private readonly HistoryService _historyService;
    private readonly CampaignService _campaignService;
    private readonly JournalPanel _journalPanel;

    private readonly FrameView _focusFrame;
    private readonly Label _focusLabel;
    private readonly Label _characterLabel;
    private readonly FrameView _actionFrame;
    private readonly Label _actionLabel;
    private readonly Button _rerollButton;
    private readonly Button _addNpcButton;

    private RandomEventResult? _lastResult;

    public RandomEventView(
        Session session,
        AdventureStateManager stateManager,
        HistoryService historyService,
        CampaignService campaignService,
        JournalPanel journalPanel)
    {
        _session = session;
        _stateManager = stateManager;
        _historyService = historyService;
        _campaignService = campaignService;
        _journalPanel = journalPanel;

        // Focus frame
        _focusFrame = new FrameView
        {
            Title = "Event Focus",
            X = Pos.Center(),
            Y = 1,
            Width = 40,
            Height = 5,
            CanFocus = true
        };

        _focusLabel = new Label
        {
            X = Pos.Center(),
            Y = 0,
            Text = ""
        };

        _characterLabel = new Label
        {
            X = Pos.Center(),
            Y = 1,
            Text = ""
        };

        _focusFrame.Add(_focusLabel, _characterLabel);

        // Action frame
        _actionFrame = new FrameView
        {
            Title = "Event Action",
            X = Pos.Center(),
            Y = Pos.Bottom(_focusFrame) + 1,
            Width = 40,
            Height = 5,
            CanFocus = true
        };

        _actionLabel = new Label
        {
            X = Pos.Center(),
            Y = 1,
            Text = ""
        };

        _actionFrame.Add(_actionLabel);

        // Buttons
        _rerollButton = new Button
        {
            X = Pos.Center() - 15,
            Y = Pos.Bottom(_actionFrame) + 1,
            Text = "[R] Re-roll",
            CanFocus = true
        };
        _rerollButton.Accepting += (s, e) => GenerateEvent();
        _rerollButton.KeyDown += (s, e) =>
        {
            if (e == Key.Enter)
            {
                GenerateEvent();
                e.Handled = true;
            }
        };

        _addNpcButton = new Button
        {
            X = Pos.Center() + 5,
            Y = Pos.Bottom(_actionFrame) + 1,
            Text = "[A] Add NPC",
            Visible = false,
            CanFocus = true
        };
        _addNpcButton.Accepting += (s, e) => AddNpc();
        _addNpcButton.KeyDown += (s, e) =>
        {
            if (e == Key.Enter)
            {
                AddNpc();
                e.Handled = true;
            }
        };

        Add(_focusFrame, _actionFrame, _rerollButton, _addNpcButton);

        _rerollButton.FocusDeepest(NavigationDirection.Forward, TabBehavior.TabStop);

        // Generate initial event
        GenerateEvent();
    }

    private void GenerateEvent()
    {
        _lastResult = RandomEvent.Generate();

        // Log the event
        var eventDetails = _lastResult.SelectedCharacter != null
            ? $"Character: {_lastResult.SelectedCharacter}"
            : _lastResult.SelectedThread != null
                ? $"Thread: {_lastResult.SelectedThread}"
                : null;

        _historyService.AddEntry(
            LogType.RandomEvent,
            $"{_lastResult.EventFocus}: {_lastResult.EventAction}",
            null,
            eventDetails
        );
        _campaignService.Save();

        // Update journal
        var entry = _historyService.Entries.LastOrDefault();
        if (entry != null)
        {
            _journalPanel.AppendEntry(entry);
        }

        // Display result
        ShowResult();
    }

    private void ShowResult()
    {
        if (_lastResult == null) return;

        _focusLabel.ColorScheme = UiThemes.Instance.ActivePrimary;
        _focusLabel.Text = _lastResult.EventFocus;

        // Show character/thread if selected
        if (_lastResult.SelectedCharacter != null)
        {
            _characterLabel.ColorScheme = UiThemes.Instance.ActivePrimary;
            _characterLabel.Text = _lastResult.SelectedCharacter;
        }
        else if (_lastResult.SelectedThread != null)
        {
            _characterLabel.ColorScheme = UiThemes.Instance.ActivePrimary;
            _characterLabel.Text = _lastResult.SelectedThread;
        }
        else if (_lastResult.ListWasEmpty)
        {
            var listType = RandomEvent.IsNpcFocus(_lastResult.EventFocus) ? "No characters" : "No threads";
            _characterLabel.ColorScheme = UiThemes.Instance.ActiveMuted;
            _characterLabel.Text = $"({listType} in list)";
        }
        else
        {
            _characterLabel.Text = "";
        }

        _actionLabel.ColorScheme = UiThemes.Instance.ActiveAccent;
        _actionLabel.Text = _lastResult.EventAction;

        // Show Add NPC button if applicable
        _addNpcButton.Visible = _lastResult.IsNewNpc;

        SetNeedsLayout();
    }

    private void AddNpc()
    {
        var dialog = new Dialog
        {
            Title = "Add NPC",
            Width = 50,
            Height = 12
        };

        var nameLabel = new Label
        {
            X = 1,
            Y = 1,
            Text = "Character Name:"
        };

        var nameField = new TextField
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(2)
        };

        var descLabel = new Label
        {
            X = 1,
            Y = 4,
            Text = "Description (optional):"
        };

        var descField = new TextField
        {
            X = 1,
            Y = 5,
            Width = Dim.Fill(2)
        };

        var errorLabel = new Label
        {
            X = 1,
            Y = 7,
            Text = "",
            ColorScheme = UiThemes.Instance.Error,
            Visible = false
        };

        var okButton = new Button
        {
            X = Pos.Center() - 10,
            Y = 8,
            Text = "Add",
            IsDefault = true
        };
        okButton.Accepting += (s, e) =>
        {
            var name = nameField.Text.ToString();
            if (string.IsNullOrWhiteSpace(name))
            {
                errorLabel.Text = "Name is required";
                errorLabel.Visible = true;
                nameField.SetFocus();
                e.Cancel = true;
                return;
            }

            var desc = descField.Text.ToString();
            _stateManager.AddCharacter(name, string.IsNullOrWhiteSpace(desc) ? null : desc);
            _campaignService.Save();
            Application.RequestStop();
        };

        var cancelButton = new Button
        {
            X = Pos.Center() + 5,
            Y = 8,
            Text = "Cancel"
        };
        cancelButton.Accepting += (s, e) => Application.RequestStop();

        dialog.Add(nameLabel, nameField, descLabel, descField, errorLabel, okButton, cancelButton);
        nameField.SetFocus();

        Application.Run(dialog);
    }
}
