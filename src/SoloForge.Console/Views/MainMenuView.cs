using Terminal.Gui;
using SoloForge.Console.App;
using SoloForge.Console.Core;
using SoloForge.Console.Services;

namespace SoloForge.Console.Views;

/// <summary>
/// Welcome dashboard showing quick reference and tips.
/// Navigation is now handled by the native MenuBar.
/// </summary>
public class MainMenuView : View
{
    private readonly Session _session;
    private readonly AdventureStateManager _stateManager;
    private readonly CampaignService _campaignService;
    private readonly SoloForgeApp _app;

    public MainMenuView(
        Session session,
        AdventureStateManager stateManager,
        CampaignService campaignService,
        SoloForgeApp app)
    {
        _session = session;
        _stateManager = stateManager;
        _campaignService = campaignService;
        _app = app;

        BuildUI();
    }

    private void BuildUI()
    {
        ColorScheme = UiThemes.Instance.ActiveDefault;

        // Welcome header
        var welcomeLabel = new Label
        {
            X = Pos.Center(),
            Y = 1,
            Text = "Welcome to SoloForge",
            ColorScheme = UiThemes.Instance.ActiveAccent
        };

        var subtitleLabel = new Label
        {
            X = Pos.Center(),
            Y = 2,
            Text = "Mythic Game Master Emulator 2e",
            ColorScheme = UiThemes.Instance.ActivePrimary
        };

        // Quick reference frame
        var quickRefFrame = new FrameView
        {
            Title = "Quick Reference",
            X = Pos.Center(),
            Y = 4,
            Width = 45,
            Height = 12,
            ColorScheme = UiThemes.Instance.ActiveDefault
        };

        var shortcuts = new Label
        {
            X = 1,
            Y = 0,
            Text = "Keyboard Shortcuts:\n\n" +
                   "  Alt+F  Fate Check      Alt+R  Random Event\n" +
                   "  Alt+S  Scene Check     Alt+M  Meaning Tables\n" +
                   "  Alt+L  Adventure Lists Alt+D  Dice Roller\n" +
                   "  Alt+J  Toggle Journal  Alt+Q  Quit\n\n" +
                   "  +/-    Adjust Chaos Factor\n" +
                   "  Esc    Return to this screen",
            ColorScheme = UiThemes.Instance.ActiveDefault
        };
        quickRefFrame.Add(shortcuts);

        // Tips frame
        var tipsFrame = new FrameView
        {
            Title = "Getting Started",
            X = Pos.Center(),
            Y = Pos.Bottom(quickRefFrame) + 1,
            Width = 45,
            Height = 8,
            ColorScheme = UiThemes.Instance.ActiveDefault
        };

        var tips = new Label
        {
            X = 1,
            Y = 0,
            Text = "1. Add characters and threads (Alt+L)\n" +
                   "2. Start a scene with Scene Check (Alt+S)\n" +
                   "3. Ask questions with Fate Check (Alt+F)\n" +
                   "4. Generate ideas with Meaning (Alt+M)\n" +
                   "5. Record everything in the Journal (Alt+J)",
            ColorScheme = UiThemes.Instance.ActiveMuted
        };
        tipsFrame.Add(tips);

        Add(welcomeLabel, subtitleLabel, quickRefFrame, tipsFrame);
    }
}
