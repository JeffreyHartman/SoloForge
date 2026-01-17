using Terminal.Gui;
using SoloForge.Console.App;
using SoloForge.Console.Core;
using SoloForge.Console.Services;

namespace SoloForge.Console.Views;

/// <summary>
/// Main menu view displaying session info and navigation options.
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
        var campaignName = _campaignService.CurrentCampaign?.Name ?? "No Campaign";

        // Session info panel
        var sessionFrame = new FrameView
        {
            Title = "Session",
            X = Pos.Center(),
            Y = 1,
            Width = 35,
            Height = 10
        };

        var sessionInfo = new Label
        {
            X = 1,
            Y = 0,
            Text = $"Campaign: {campaignName}\n" +
                   $"Engine:   {_session.Engine}\n" +
                   $"Theme:    {_session.Theme}\n" +
                   $"Chaos:    {_session.Chaos}\n" +
                   $"─────────────────────\n" +
                   $"Characters: {_stateManager.CharacterCount}\n" +
                   $"Threads:    {_stateManager.ActiveThreadCount}"
        };
        sessionFrame.Add(sessionInfo);

        // Menu buttons
        var menuFrame = new FrameView
        {
            Title = "Actions",
            X = Pos.Center(),
            Y = Pos.Bottom(sessionFrame) + 1,
            Width = 35,
            Height = 14
        };

        var y = 0;
        var buttons = new (string label, Action? action)[]
        {
            ("[F] Fate Check", () => _app.ShowFateCheck()),
            ("[R] Random Event", () => _app.ShowRandomEvent()),
            ("[S] Scene Check", () => _app.ShowSceneCheck()),
            ("[M] Discovering Meaning", () => _app.ShowMeaning()),
            ("[L] Adventure Lists", () => _app.ShowAdventureLists()),
            ("[D] Dice Roller", () => _app.ShowDiceRoller()),
            ("─────────────────────────", null),
            ("[G] Game Manager", () => _app.ShowGameManager()),
            ("─────────────────────────", null),
            ("[Q] Quit", () => Application.RequestStop())
        };

        foreach (var (label, action) in buttons)
        {
            if (action == null)
            {
                // Separator
                var sep = new Label
                {
                    X = 1,
                    Y = y,
                    Text = label
                };
                menuFrame.Add(sep);
            }
            else
            {
                var btn = new Button
                {
                    X = 1,
                    Y = y,
                    Text = label,
                    IsDefault = y == 0
                };
                btn.Accepting += (s, e) => action();
                menuFrame.Add(btn);
            }
            y++;
        }

        Add(sessionFrame, menuFrame);
    }
}
