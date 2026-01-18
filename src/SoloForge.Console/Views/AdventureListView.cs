using System.Collections.ObjectModel;
using Terminal.Gui;
using SoloForge.Console.App;
using SoloForge.Console.Services;

namespace SoloForge.Console.Views;

/// <summary>
/// View for managing adventure lists (characters and threads).
/// </summary>
public class AdventureListView : View
{
    private readonly AdventureStateManager _stateManager;
    private readonly CampaignService _campaignService;

    private readonly ListView _characterList;
    private readonly ListView _threadList;

    public AdventureListView(AdventureStateManager stateManager, CampaignService campaignService)
    {
        _stateManager = stateManager;
        _campaignService = campaignService;

        // Characters frame
        var charactersFrame = new FrameView
        {
            Title = "Characters",
            X = 0,
            Y = 0,
            Width = Dim.Percent(50),
            Height = Dim.Fill(4),
            ColorScheme = UiThemes.Instance.ActiveDefault
        };

        _characterList = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        _characterList.OpenSelectedItem += (s, e) => AddCharacter();
        _characterList.TabStop = TabBehavior.TabStop;
        _characterList.CanFocus = true;
        _characterList.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Enter)
            {
                AddCharacter();
                e.Handled = true;
            }
        };
        charactersFrame.Add(_characterList);

        // Threads frame
        var threadsFrame = new FrameView
        {
            Title = "Threads",
            X = Pos.Right(charactersFrame),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(4),
            ColorScheme = UiThemes.Instance.ActiveDefault
        };

        _threadList = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        _threadList.OpenSelectedItem += (s, e) => ToggleThread();
        _threadList.TabStop = TabBehavior.TabStop;
        _threadList.CanFocus = true;
        _threadList.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Enter)
            {
                ToggleThread();
                e.Handled = true;
            }
        };
        threadsFrame.Add(_threadList);

        // Button bar
        var addCharBtn = new Button
        {
            X = 1,
            Y = Pos.AnchorEnd(3),
            Text = "[A] Add Character",
            CanFocus = true
        };
        addCharBtn.Accepting += (s, e) => AddCharacter();

        var removeCharBtn = new Button
        {
            X = Pos.Right(addCharBtn) + 1,
            Y = Pos.AnchorEnd(3),
            Text = "[X] Remove",
            CanFocus = true
        };
        removeCharBtn.Accepting += (s, e) => RemoveCharacter();

        var addThreadBtn = new Button
        {
            X = Pos.Percent(50) + 1,
            Y = Pos.AnchorEnd(3),
            Text = "[T] Add Thread",
            CanFocus = true
        };
        addThreadBtn.Accepting += (s, e) => AddThread();

        var toggleThreadBtn = new Button
        {
            X = Pos.Right(addThreadBtn) + 1,
            Y = Pos.AnchorEnd(3),
            Text = "[R] Resolve",
            CanFocus = true
        };
        toggleThreadBtn.Accepting += (s, e) => ToggleThread();

        Add(charactersFrame, threadsFrame, addCharBtn, removeCharBtn, addThreadBtn, toggleThreadBtn);

        RefreshLists();

        // Keyboard shortcuts
        KeyDown += (s, e) =>
        {
            switch (e.KeyCode)
            {
                case KeyCode.A:
                    AddCharacter();
                    e.Handled = true;
                    break;
                case KeyCode.X:
                    RemoveCharacter();
                    e.Handled = true;
                    break;
                case KeyCode.T:
                    AddThread();
                    e.Handled = true;
                    break;
                case KeyCode.R:
                    ToggleThread();
                    e.Handled = true;
                    break;
            }
        };

        _characterList.FocusDeepest(NavigationDirection.Forward, TabBehavior.TabStop);
        if (_stateManager.State.Characters.Count > 0)
        {
            _characterList.SelectedItem = Math.Min(_characterList.SelectedItem, _stateManager.State.Characters.Count - 1);
        }
        else
        {
            _characterList.SelectedItem = 0;
        }
    }

    private void RefreshLists()
    {
        var characters = _stateManager.State.Characters
            .Select(c => string.IsNullOrEmpty(c.Description) ? c.Name : $"{c.Name} - {c.Description}")
            .ToList();

        // Combine active and closed threads for display
        var allThreads = _stateManager.State.ActiveThreads
            .Select(t => string.IsNullOrEmpty(t.Description) ? t.Name : $"{t.Name} - {t.Description}")
            .Concat(_stateManager.State.ClosedThreads
                .Select(t =>
                {
                    var name = string.IsNullOrEmpty(t.Description) ? t.Name : $"{t.Name} - {t.Description}";
                    return $"[Closed] {name}";
                }))
            .ToList();

        _characterList.SetSource(new ObservableCollection<string>(characters.Count > 0 ? characters : ["(No characters)"]));
        _threadList.SetSource(new ObservableCollection<string>(allThreads.Count > 0 ? allThreads : ["(No threads)"]));

        if (characters.Count == 0)
        {
            _characterList.SelectedItem = 0;
        }
        else if (_characterList.SelectedItem >= characters.Count)
        {
            _characterList.SelectedItem = characters.Count - 1;
        }

        if (allThreads.Count == 0)
        {
            _threadList.SelectedItem = 0;
        }
        else if (_threadList.SelectedItem >= allThreads.Count)
        {
            _threadList.SelectedItem = allThreads.Count - 1;
        }
    }

    private void AddCharacter()
    {
        var dialog = new Dialog
        {
            Title = "Add Character",
            Width = 50,
            Height = 12
        };

        var nameLabel = new Label
        {
            X = 1,
            Y = 1,
            Text = "Name:"
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

        var okButton = new Button { Text = "Add", IsDefault = true };
        okButton.Accepting += (s, e) =>
        {
            var name = nameField.Text.ToString();
            if (!string.IsNullOrWhiteSpace(name))
            {
                var desc = descField.Text.ToString();
                _stateManager.AddCharacter(name, string.IsNullOrWhiteSpace(desc) ? null : desc);
                _campaignService.Save();
                RefreshLists();
            }
            Application.RequestStop();
        };

        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Accepting += (s, e) => Application.RequestStop();

        dialog.Add(nameLabel, nameField, descLabel, descField);
        dialog.AddButton(okButton);
        dialog.AddButton(cancelButton);
        nameField.SetFocus();

        Application.Run(dialog);
    }

    private void RemoveCharacter()
    {
        if (_stateManager.State.Characters.Count == 0) return;

        var idx = _characterList.SelectedItem;
        if (idx < 0 || idx >= _stateManager.State.Characters.Count) return;

        var character = _stateManager.State.Characters[idx];
        var result = MessageBox.Query("Remove Character", $"Remove '{character.Name}'?", "Yes", "No");
        if (result == 0)
        {
            _stateManager.RemoveCharacter(character);
            _campaignService.Save();
            RefreshLists();
        }
    }

    private void AddThread()
    {
        var dialog = new Dialog
        {
            Title = "Add Thread",
            Width = 50,
            Height = 12
        };

        var nameLabel = new Label
        {
            X = 1,
            Y = 1,
            Text = "Name:"
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

        var okButton = new Button { Text = "Add", IsDefault = true };
        okButton.Accepting += (s, e) =>
        {
            var name = nameField.Text.ToString();
            if (!string.IsNullOrWhiteSpace(name))
            {
                var desc = descField.Text.ToString();
                _stateManager.AddThread(name, string.IsNullOrWhiteSpace(desc) ? null : desc);
                _campaignService.Save();
                RefreshLists();
            }
            Application.RequestStop();
        };

        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Accepting += (s, e) => Application.RequestStop();

        dialog.Add(nameLabel, nameField, descLabel, descField);
        dialog.AddButton(okButton);
        dialog.AddButton(cancelButton);
        nameField.SetFocus();

        Application.Run(dialog);
    }

    private void ToggleThread()
    {
        var activeCount = _stateManager.State.ActiveThreads.Count;
        var closedCount = _stateManager.State.ClosedThreads.Count;
        var totalCount = activeCount + closedCount;

        if (totalCount == 0) return;

        var idx = _threadList.SelectedItem;
        if (idx < 0 || idx >= totalCount) return;

        if (idx < activeCount)
        {
            // Toggle active thread to closed
            var thread = _stateManager.State.ActiveThreads[idx];
            _stateManager.CloseThread(thread);
        }
        else
        {
            // Toggle closed thread to active
            var thread = _stateManager.State.ClosedThreads[idx - activeCount];
            _stateManager.ReopenThread(thread);
        }

        _campaignService.Save();
        RefreshLists();
    }
}
