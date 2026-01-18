using Serilog;
using Terminal.Gui;
using SoloForge.Console.Core;
using SoloForge.Console.Services;
using SoloForge.Console.Views;
using SoloForge.Console.Views.Components;

namespace SoloForge.Console.App;

/// <summary>
/// Main application window for SoloForge using Terminal.Gui.
/// Manages the split-pane layout with swappable content views and persistent journal.
/// </summary>
public class SoloForgeApp : Toplevel
{
    private readonly Session _session;
    private readonly AdventureStateManager _stateManager;
    private readonly HistoryService _historyService;
    private readonly CampaignService _campaignService;
    private readonly ILogger _log = AppLogger.ForContext<SoloForgeApp>();

    private readonly MenuBar _menuBar;
    private readonly CampaignInfoPanel _campaignInfoPanel;
    private readonly FrameView _contentFrame;
    private readonly LiveLogPanel _liveLogPanel;
    private readonly FrameView _journalFrame;
    private readonly JournalView _journalView;
    private readonly StatusBar _statusBar;

    private View? _currentContentView;
    private bool _journalVisible = false; // Start with Live Log visible

    public SoloForgeApp(
        Session session,
        AdventureStateManager stateManager,
        HistoryService historyService,
        CampaignService campaignService)
    {
        _session = session;
        _stateManager = stateManager;
        _historyService = historyService;
        _campaignService = campaignService;

        Title = "SoloForge - Mythic GME 2e";
        ColorScheme = UiThemes.Instance.ActiveDefault;

        // Create native MenuBar
        _menuBar = new MenuBar
        {
            Menus =
            [
                new MenuBarItem("_File",
                [
                    new MenuItem("_New Campaign", "", () => CreateCampaign()),
                    new MenuItem("_Switch Campaign", "", () => SwitchCampaign()),
                    null!, // separator
                    new MenuItem("_Quit", "", () => RequestQuit(), shortcutKey: Key.Q.WithAlt)
                ]),
                new MenuBarItem("_Engine",
                [
                    new MenuItem("_Fate Check", "", ShowFateCheck, shortcutKey: Key.F.WithAlt),
                    new MenuItem("_Random Event", "", ShowRandomEvent, shortcutKey: Key.R.WithAlt),
                    new MenuItem("_Scene Check", "", ShowSceneCheck, shortcutKey: Key.S.WithAlt)
                ]),
                new MenuBarItem("_Meaning",
                [
                    new MenuItem("_Action", "Quick roll action meaning", () => QuickMeaning("Action")),
                    new MenuItem("_Description", "Quick roll description meaning", () => QuickMeaning("Description")),
                    null!, // separator
                    new MenuItem("_Element Tables...", "", ShowMeaning, shortcutKey: Key.M.WithAlt)
                ]),
                new MenuBarItem("_Tracking",
                [
                    new MenuItem("Adventure _Lists", "", ShowAdventureLists, shortcutKey: Key.L.WithAlt),
                    new MenuItem("_Journal", "Toggle journal pane", ToggleJournal, shortcutKey: Key.J.WithAlt)
                ]),
                new MenuBarItem("T_ools",
                [
                    new MenuItem("_Dice Roller", "", ShowDiceRoller, shortcutKey: Key.D.WithAlt),
                    null!, // separator
                    new MenuItem("_Game Manager", "", ShowGameManager, shortcutKey: Key.G.WithAlt)
                ]),
                new MenuBarItem("_Themes", BuildThemeMenuItems())
            ],
            ColorScheme = UiThemes.Instance.ActiveMenu
        };

        // Subscribe to theme changes
        UiThemes.Instance.ThemeChanged += OnThemeChanged;

        // Create 3-column layout below MenuBar
        // Left: Campaign Info Panel (20%)
        _campaignInfoPanel = new CampaignInfoPanel(session, stateManager, campaignService)
        {
            X = 0,
            Y = Pos.Bottom(_menuBar),
            Width = Dim.Percent(20),
            Height = Dim.Fill(1)
        };

        // Center: Content/Workspace Frame (50%)
        _contentFrame = new FrameView
        {
            Title = "Main Menu",
            X = Pos.Right(_campaignInfoPanel),
            Y = Pos.Bottom(_menuBar),
            Width = Dim.Percent(50),
            Height = Dim.Fill(1),
            BorderStyle = LineStyle.Double,
            ColorScheme = UiThemes.Instance.ActiveDefault,
            CanFocus = true
        };

        // Right: Live Log Panel (30%)
        _liveLogPanel = new LiveLogPanel(historyService)
        {
            X = Pos.Right(_contentFrame),
            Y = Pos.Bottom(_menuBar),
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };

        // Create journal frame (hidden by default, overlays live log when visible)
        _journalFrame = new FrameView
        {
            Title = "Journal",
            X = Pos.Right(_contentFrame),
            Y = Pos.Bottom(_menuBar),
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            BorderStyle = LineStyle.Double,
            ColorScheme = UiThemes.Instance.ActiveDefault,
            CanFocus = true,
            Visible = false
        };

        // Create journal view
        _journalView = new JournalView(historyService, campaignService)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true
        };
        _journalFrame.Add(_journalView);

        // Create native StatusBar at bottom
        _statusBar = new StatusBar
        {
            Visible = true,
            ColorScheme = UiThemes.Instance.ActiveMenu
        };
        _statusBar.Add(
            new Shortcut { Title = "Fate", Key = Key.F.WithAlt, Action = ShowFateCheck },
            new Shortcut { Title = "Random", Key = Key.R.WithAlt, Action = ShowRandomEvent },
            new Shortcut { Title = "Scene", Key = Key.S.WithAlt, Action = ShowSceneCheck },
            new Shortcut { Title = "Meaning", Key = Key.M.WithAlt, Action = ShowMeaning },
            new Shortcut { Title = "Lists", Key = Key.L.WithAlt, Action = ShowAdventureLists },
            new Shortcut { Title = "Journal", Key = Key.J.WithAlt, Action = ToggleJournal },
            new Shortcut { Title = "Chaos+", Key = Key.D0.WithShift, Action = IncreaseChaos },
            new Shortcut { Title = "Chaos-", Key = (Key)'-', Action = DecreaseChaos }
        );

        Add(_menuBar, _campaignInfoPanel, _contentFrame, _liveLogPanel, _journalFrame, _statusBar);

        // Show main menu initially
        ShowMainMenu();

        // Set up remaining keyboard shortcuts
        SetupKeyBindings();
    }

    private void SetupKeyBindings()
    {
        // Simplified key bindings - MenuBar handles Alt+key shortcuts
        Application.KeyDown += (s, e) =>
        {
            if (Application.Top != this)
            {
                return;
            }

            var baseKey = e.KeyCode & ~KeyCode.AltMask & ~KeyCode.ShiftMask & ~KeyCode.CtrlMask;
            var focused = Application.Top?.Focused;
            var inTextField = focused is TextField || focused is TextView;

            // Escape returns to main menu (unless in a dialog)
            if (baseKey == KeyCode.Esc && !inTextField)
            {
                ShowMainMenu();
                e.Handled = true;
                return;
            }

            // Tab to switch focus between content and journal
            if (baseKey == KeyCode.Tab)
            {
                try
                {
                    if (TryToggleFocusBetweenPanes())
                    {
                        e.Handled = true;
                    }
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "Failed to toggle focus between panes");
                }
            }
        };
    }

    private void IncreaseChaos()
    {
        var newValue = _session.Chaos + 1;
        _session.Chaos = Math.Clamp(newValue, 1, 9);
        _campaignInfoPanel.Refresh();
    }

    private void DecreaseChaos()
    {
        var newValue = _session.Chaos - 1;
        _session.Chaos = Math.Clamp(newValue, 1, 9);
        _campaignInfoPanel.Refresh();
    }

    private void CreateCampaign()
    {
        var dialog = new Dialog
        {
            Title = "New Campaign",
            Width = 50,
            Height = 10
        };

        var nameLabel = new Label
        {
            X = 1,
            Y = 1,
            Text = "Campaign Name:"
        };

        var nameField = new TextField
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(2)
        };

        var okButton = new Button { Text = "Create", IsDefault = true };
        okButton.Accepting += (s, e) =>
        {
            var name = nameField.Text.ToString();
            if (!string.IsNullOrWhiteSpace(name))
            {
                _campaignService.CreateNew(name);
                _journalView.ReloadForCampaign();
                RefreshSessionInfo();
            }
            Application.RequestStop();
        };

        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Accepting += (s, e) => Application.RequestStop();

        dialog.Add(nameLabel, nameField);
        dialog.AddButton(okButton);
        dialog.AddButton(cancelButton);
        nameField.SetFocus();

        Application.Run(dialog);
    }

    private void SwitchCampaign()
    {
        var campaigns = _campaignService.ListCampaigns().ToList();

        if (campaigns.Count == 0)
        {
            MessageBox.Query("Switch Campaign", "No campaigns found.", "OK");
            return;
        }

        if (campaigns.Count == 1)
        {
            MessageBox.Query("Switch Campaign", "Only one campaign exists. Create another to switch.", "OK");
            return;
        }

        var dialog = new Dialog
        {
            Title = "Switch Campaign",
            Width = Dim.Percent(70),
            Height = Dim.Percent(60)
        };

        dialog.KeyDown += (s, e) =>
        {
            if (e.KeyCode == KeyCode.Esc)
            {
                Application.RequestStop();
                e.Handled = true;
            }
        };

        var campaignNames = new System.Collections.ObjectModel.ObservableCollection<string>(
            campaigns.Select(c =>
            {
                var current = _campaignService.CurrentCampaign?.Id == c.Id ? " (current)" : "";
                return $"{c.Name}{current} - {c.LastPlayed:MMM d}";
            }));

        var listView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(3)
        };
        listView.SetSource(campaignNames);

        Models.CampaignData? selected = null;
        listView.OpenSelectedItem += (s, e) =>
        {
            if (listView.SelectedItem >= 0 && listView.SelectedItem < campaigns.Count)
            {
                selected = campaigns[listView.SelectedItem];
                Application.RequestStop();
            }
        };

        var okButton = new Button { Text = "Switch", IsDefault = true };
        okButton.Accepting += (s, e) =>
        {
            if (listView.SelectedItem >= 0 && listView.SelectedItem < campaigns.Count)
            {
                selected = campaigns[listView.SelectedItem];
                Application.RequestStop();
            }
        };

        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Accepting += (s, e) => Application.RequestStop();

        dialog.Add(listView);
        dialog.AddButton(okButton);
        dialog.AddButton(cancelButton);

        Application.Run(dialog);

        if (selected != null && selected.Id != _campaignService.CurrentCampaign?.Id)
        {
            _campaignService.Load(selected.Id);
            _journalView.ReloadForCampaign();
            RefreshSessionInfo();
        }
    }

    private void QuickMeaning(string tableType)
    {
        var tableService = TableService.Instance;
        var tables = tableService.AvailableTables
            .Where(t => t.DisplayName.Contains(tableType, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (tables.Count == 0)
        {
            MessageBox.Query("Quick Meaning", $"No {tableType} tables found.", "OK");
            return;
        }

        // Find the primary table (exact match or first containing)
        var primaryTable = tables.FirstOrDefault(t =>
            t.DisplayName.Equals(tableType, StringComparison.OrdinalIgnoreCase)) ?? tables[0];

        var result1 = tableService.GetRandomWord(primaryTable.Id);
        var result2 = tableService.GetRandomWord(primaryTable.Id);

        var resultText = $"{result1} + {result2}";
        var entry = _historyService.AddEntry(
            Models.LogType.Meaning,
            resultText,
            context: $"{tableType} Quick Roll",
            details: $"Table: {primaryTable.DisplayName}"
        );

        _journalView.AppendEntry(entry);
        _campaignService.Save();

        MessageBox.Query($"{tableType} Meaning", resultText, "OK");
    }

    private void ToggleJournal()
    {
        _journalVisible = !_journalVisible;

        if (_journalVisible)
        {
            // Show journal, hide live log
            _liveLogPanel.Visible = false;
            _journalFrame.Visible = true;
        }
        else
        {
            // Show live log, hide journal
            _journalFrame.Visible = false;
            _liveLogPanel.Visible = true;
            _liveLogPanel.Refresh();
        }

        SetNeedsLayout();
    }

    private bool TryToggleFocusBetweenPanes()
    {
        if (!_journalVisible)
        {
            return false;
        }

        if (_journalFrame.Visible == false || _contentFrame.Visible == false)
        {
            return false;
        }

        var focused = Application.Top?.Focused;
        if (focused == null)
        {
            return false;
        }

        var inContent = ApplicationNavigation.IsInHierarchy(_contentFrame, focused);
        var inJournal = ApplicationNavigation.IsInHierarchy(_journalFrame, focused);
        if (!inContent && !inJournal)
        {
            return false;
        }

        var isTextInput = focused is TextField || focused is TextView;
        if (isTextInput && !inJournal)
        {
            return false;
        }

        var target = _journalView.HasFocus ? _currentContentView : _journalView;
        if (!inContent && inJournal && _currentContentView != null)
        {
            target = _currentContentView;
        }
        else if (inContent && !inJournal)
        {
            target = _journalView;
        }
        if (target == null)
        {
            _log.Warning("Tab focus toggle skipped because target view was null");
            return false;
        }

        if (target.CanFocus)
        {
            try
            {
                if (target.FocusDeepest(NavigationDirection.Forward, TabBehavior.TabStop))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Failed to focus deepest view");
            }
        }

        try
        {
            return target.SetFocus();
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Tab focus toggle failed while setting focus");
            return false;
        }
    }

    private void SetContentView(View view, string title)
    {
        if (_currentContentView != null)
        {
            _contentFrame.Remove(_currentContentView);
            _currentContentView.Dispose();
        }

        _currentContentView = view;
        _currentContentView.CanFocus = true;
        _currentContentView.X = 0;
        _currentContentView.Y = 0;
        _currentContentView.Width = Dim.Fill();
        _currentContentView.Height = Dim.Fill();

        _contentFrame.Title = title;
        _contentFrame.Add(_currentContentView);

        var preferredFocus = _currentContentView.MostFocused ?? _currentContentView.Focused;
        if (preferredFocus != null)
        {
            preferredFocus.SetFocus();
            return;
        }

        if (!_currentContentView.FocusDeepest(NavigationDirection.Forward, TabBehavior.TabStop))
        {
            _currentContentView.SetFocus();
        }
    }

    public void ShowMainMenu()
    {
        var view = new MainMenuView(_session, _stateManager, _campaignService, this);
        SetContentView(view, "Main Menu");
    }

    public void ShowFateCheck()
    {
        var view = new FateCheckView(_session, _historyService, _campaignService, _journalView);
        SetContentView(view, "Fate Check");
    }

    public void ShowSceneCheck()
    {
        var view = new SceneCheckView(_session, _historyService, _campaignService, _journalView);
        SetContentView(view, "Scene Check");
    }

    public void ShowRandomEvent()
    {
        var view = new RandomEventView(_session, _stateManager, _historyService, _campaignService, _journalView);
        SetContentView(view, "Random Event");
    }

    public void ShowMeaning()
    {
        var view = new MeaningView(_session, _historyService, _campaignService, _journalView);
        SetContentView(view, "Discovering Meaning");
    }

    public void ShowAdventureLists()
    {
        var view = new AdventureListView(_stateManager, _campaignService);
        SetContentView(view, "Adventure Lists");
    }

    public void ShowDiceRoller()
    {
        var view = new DiceRollerView(_historyService, _campaignService, _journalView);
        SetContentView(view, "Dice Roller");
    }

    public void ShowGameManager()
    {
        var view = new GameManagerView(_campaignService, _journalView, this);
        SetContentView(view, "Game Manager");
    }

    private void RequestQuit()
    {
        var result = MessageBox.Query("Quit", "Are you sure you want to quit?", "Yes", "No");
        if (result == 0)
        {
            Application.RequestStop();
        }
    }

    public void RefreshSessionInfo()
    {
        _campaignInfoPanel.Refresh();
        _liveLogPanel.Refresh();
    }

    public void RefreshJournal()
    {
        _journalView.Refresh();
    }

    private MenuItem[] BuildThemeMenuItems()
    {
        var items = new List<MenuItem>();

        // Add theme selection items
        foreach (var themeName in ThemeService.Instance.AvailableThemes)
        {
            var name = themeName; // Capture for closure
            var isActive = themeName == ThemeService.Instance.ActiveThemeName;
            var marker = isActive ? "● " : "  ";
            items.Add(new MenuItem($"{marker}{themeName}", "", () => SelectTheme(name)));
        }

        return items.ToArray();
    }

    private void SelectTheme(string themeName)
    {
        if (UiThemes.Instance.ApplyTheme(themeName))
        {
            _log.Information("Theme changed to: {ThemeName}", themeName);
            RebuildThemeMenu();
        }
    }

    private void RebuildThemeMenu()
    {
        // Find and update the Themes menu
        var themesMenu = _menuBar.Menus.FirstOrDefault(m => m.Title.ToString()?.Contains("Themes") == true);
        if (themesMenu != null)
        {
            themesMenu.Children = BuildThemeMenuItems();
        }
    }

    private void OnThemeChanged()
    {
        // Update color schemes on all UI components
        Application.Invoke(() =>
        {
            ColorScheme = UiThemes.Instance.ActiveDefault;
            _menuBar.ColorScheme = UiThemes.Instance.ActiveMenu;
            _contentFrame.ColorScheme = UiThemes.Instance.ActiveDefault;
            _journalFrame.ColorScheme = UiThemes.Instance.ActiveDefault;
            _journalView.ColorScheme = UiThemes.Instance.ActiveDefault;
            _statusBar.ColorScheme = UiThemes.Instance.ActiveMenu;
            _campaignInfoPanel.Refresh();
            _liveLogPanel.Refresh();

            // Rebuild content view to apply new theme colors
            ShowMainMenu();

            this.SetNeedsLayout();
        });
    }

}
