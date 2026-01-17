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

    private readonly FrameView _contentFrame;
    private readonly FrameView _journalFrame;
    private readonly JournalView _journalView;
    private readonly SessionInfoBar _sessionInfoBar;
    private readonly Label _statusBar;

    private View? _currentContentView;
    private bool _journalVisible = true;

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
        ColorScheme = UiThemes.Instance.Default;

        // Create session info bar at top
        _sessionInfoBar = new SessionInfoBar(session, stateManager, campaignService)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1
        };

        // Create content frame (left pane)
        _contentFrame = new FrameView
        {
            Title = "Main Menu",
            X = 0,
            Y = 1,
            Width = Dim.Percent(60),
            Height = Dim.Fill(1),
            ColorScheme = ColorScheme,
            CanFocus = true
        };

        // Create journal frame (right pane)
        _journalFrame = new FrameView
        {
            Title = "Journal",
            X = Pos.Right(_contentFrame),
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            ColorScheme = ColorScheme,
            CanFocus = true
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

        // Create status bar at bottom
        _statusBar = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
            Text = "Alt: F=Fate R=Random S=Scene M=Meaning L=Lists D=Dice G=Game J=Journal | +/- Chaos | Esc=Menu | Tab=Focus"
        };

        Add(_sessionInfoBar, _contentFrame, _journalFrame, _statusBar);

        // Show main menu initially
        ShowMainMenu();

        // Set up keyboard shortcuts
        SetupKeyBindings();
    }

    private void SetupKeyBindings()
    {
        // Global key bindings - all navigation uses Alt modifier
        Application.KeyDown += (s, e) =>
        {
            if (Application.Top != this)
            {
                return;
            }

            // Check for Alt modifier for navigation shortcuts
            var hasAlt = e.KeyCode.HasFlag(KeyCode.AltMask);
            var baseKey = e.KeyCode & ~KeyCode.AltMask & ~KeyCode.ShiftMask & ~KeyCode.CtrlMask;
            var focused = Application.Top?.Focused;
            var inTextField = focused is TextField || focused is TextView;
            var isTab = baseKey == KeyCode.Tab;
            var isEscape = baseKey == KeyCode.Esc;
            var isChaosIncrease = baseKey == (KeyCode)'+' ||
                baseKey == (KeyCode)'=' && e.KeyCode.HasFlag(KeyCode.ShiftMask);
            var isChaosDecrease = baseKey == (KeyCode)'-';
            var isChaosKey = isChaosIncrease || isChaosDecrease;

            if (!hasAlt && !isTab && !isEscape && !isChaosKey && focused is ListView)
            {
                return;
            }

            if (hasAlt)
            {
                switch (baseKey)
                {
                    case KeyCode.J:
                        ToggleJournal();
                        e.Handled = true;
                        return;
                    case KeyCode.F:
                        ShowFateCheck();
                        e.Handled = true;
                        return;
                    case KeyCode.R:
                        ShowRandomEvent();
                        e.Handled = true;
                        return;
                    case KeyCode.S:
                        ShowSceneCheck();
                        e.Handled = true;
                        return;
                    case KeyCode.M:
                        ShowMeaning();
                        e.Handled = true;
                        return;
                    case KeyCode.L:
                        ShowAdventureLists();
                        e.Handled = true;
                        return;
                    case KeyCode.D:
                        ShowDiceRoller();
                        e.Handled = true;
                        return;
                    case KeyCode.G:
                        ShowGameManager();
                        e.Handled = true;
                        return;
                    case KeyCode.Q:
                        RequestQuit();
                        e.Handled = true;
                        return;
                }
            }

            // Escape returns to main menu
            if (baseKey == KeyCode.Esc)
            {
                ShowMainMenu();
                e.Handled = true;
                return;
            }

            if (!inTextField)
            {
                // Handle + key (Shift+= on US keyboards, or numpad +)
                if (isChaosIncrease)
                {
                    _session.Chaos++;
                    _sessionInfoBar.Refresh();
                    e.Handled = true;
                    return;
                }

                // Handle - key
                if (isChaosDecrease)
                {
                    _session.Chaos--;
                    _sessionInfoBar.Refresh();
                    e.Handled = true;
                    return;
                }
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

    private void ToggleJournal()
    {
        _journalVisible = !_journalVisible;

        if (_journalVisible)
        {
            _contentFrame.Width = Dim.Percent(60);
            _journalFrame.Visible = true;
        }
        else
        {
            _contentFrame.Width = Dim.Fill();
            _journalFrame.Visible = false;
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
        _sessionInfoBar.Refresh();
    }

    public void RefreshJournal()
    {
        _journalView.Refresh();
    }

}
