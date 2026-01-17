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
        ColorScheme = CreateColorScheme();

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
            ColorScheme = ColorScheme
        };

        // Create journal frame (right pane)
        _journalFrame = new FrameView
        {
            Title = "Journal",
            X = Pos.Right(_contentFrame),
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            ColorScheme = ColorScheme
        };

        // Create journal view
        _journalView = new JournalView(historyService, campaignService)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        _journalFrame.Add(_journalView);

        // Create status bar at bottom
        _statusBar = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
            Text = "[F]ate [R]andom [S]cene [M]eaning [L]ists [D]ice [G]ame | Alt+J Journal | +/- Chaos | Q Quit"
        };

        Add(_sessionInfoBar, _contentFrame, _journalFrame, _statusBar);

        // Show main menu initially
        ShowMainMenu();

        // Set up keyboard shortcuts
        SetupKeyBindings();
    }

    private void SetupKeyBindings()
    {
        // Global key bindings
        KeyDown += (s, e) =>
        {
            // Alt+J toggles journal
            if (e.KeyCode == (KeyCode.J | KeyCode.AltMask))
            {
                ToggleJournal();
                e.Handled = true;
                return;
            }

            // Chaos adjustment with + and -
            if (e.KeyCode == KeyCode.D0 + 11 || e.KeyCode == (KeyCode.ShiftMask | KeyCode.D0 + 11)) // + key
            {
                _session.Chaos++;
                _sessionInfoBar.Refresh();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == KeyCode.D0 + 13) // - key
            {
                _session.Chaos--;
                _sessionInfoBar.Refresh();
                e.Handled = true;
                return;
            }

            // Navigation shortcuts (when not in a text field)
            if (!IsInTextInput())
            {
                switch (e.KeyCode)
                {
                    case KeyCode.F:
                        ShowFateCheck();
                        e.Handled = true;
                        break;
                    case KeyCode.R:
                        ShowRandomEvent();
                        e.Handled = true;
                        break;
                    case KeyCode.S:
                        ShowSceneCheck();
                        e.Handled = true;
                        break;
                    case KeyCode.M:
                        ShowMeaning();
                        e.Handled = true;
                        break;
                    case KeyCode.L:
                        ShowAdventureLists();
                        e.Handled = true;
                        break;
                    case KeyCode.D:
                        ShowDiceRoller();
                        e.Handled = true;
                        break;
                    case KeyCode.G:
                        ShowGameManager();
                        e.Handled = true;
                        break;
                    case KeyCode.Q:
                        RequestQuit();
                        e.Handled = true;
                        break;
                    case KeyCode.Esc:
                        ShowMainMenu();
                        e.Handled = true;
                        break;
                }
            }
        };
    }

    private bool IsInTextInput()
    {
        var focused = Application.Top?.MostFocused;
        return focused is TextField or TextView;
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

    private void SetContentView(View view, string title)
    {
        if (_currentContentView != null)
        {
            _contentFrame.Remove(_currentContentView);
            _currentContentView.Dispose();
        }

        _currentContentView = view;
        _currentContentView.X = 0;
        _currentContentView.Y = 0;
        _currentContentView.Width = Dim.Fill();
        _currentContentView.Height = Dim.Fill();

        _contentFrame.Title = title;
        _contentFrame.Add(_currentContentView);
        _currentContentView.SetFocus();
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

    private static ColorScheme CreateColorScheme()
    {
        return new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute(Color.White, Color.Black),
            Focus = new Terminal.Gui.Attribute(Color.Black, Color.Cyan),
            HotNormal = new Terminal.Gui.Attribute(Color.Cyan, Color.Black),
            HotFocus = new Terminal.Gui.Attribute(Color.Black, Color.Cyan),
            Disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Black)
        };
    }
}
