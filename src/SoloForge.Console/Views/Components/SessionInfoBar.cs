using Terminal.Gui;
using SoloForge.Console.App;
using SoloForge.Console.Core;
using SoloForge.Console.Services;

namespace SoloForge.Console.Views.Components;

/// <summary>
/// Status bar displaying campaign name, chaos factor, character/thread counts.
/// Uses segmented colored labels for visual distinction.
/// </summary>
public class SessionInfoBar : View
{
    private readonly Session _session;
    private readonly AdventureStateManager _stateManager;
    private readonly CampaignService _campaignService;

    private readonly Label _appLabel;
    private readonly Label _campaignLabelKey;
    private readonly Label _campaignLabelValue;
    private readonly Label _chaosLabelKey;
    private readonly Label _chaosLabelValue;
    private readonly Label _charactersLabelKey;
    private readonly Label _charactersLabelValue;
    private readonly Label _threadsLabelKey;
    private readonly Label _threadsLabelValue;
    private readonly Label _spacer;

    public SessionInfoBar(Session session, AdventureStateManager stateManager, CampaignService campaignService)
    {
        _session = session;
        _stateManager = stateManager;
        _campaignService = campaignService;

        // App name - always visible
        _appLabel = new Label
        {
            X = 0,
            Y = 0,
            Text = " SoloForge ",
            ColorScheme = UiThemes.Instance.SessionHeader
        };

        // Separator
        var sep1 = new Label
        {
            X = Pos.Right(_appLabel),
            Y = 0,
            Text = "|",
            ColorScheme = UiThemes.Instance.SessionHeader
        };

        // Campaign label
        _campaignLabelKey = new Label
        {
            X = Pos.Right(sep1),
            Y = 0,
            Text = " Campaign: ",
            ColorScheme = UiThemes.Instance.SessionHeader
        };

        _campaignLabelValue = new Label
        {
            X = Pos.Right(_campaignLabelKey),
            Y = 0,
            Text = "",
            ColorScheme = UiThemes.Instance.SessionHeaderValue
        };

        // Separator
        var sep2 = new Label
        {
            X = Pos.Right(_campaignLabelValue),
            Y = 0,
            Text = " |",
            ColorScheme = UiThemes.Instance.SessionHeader
        };

        // Chaos label
        _chaosLabelKey = new Label
        {
            X = Pos.Right(sep2),
            Y = 0,
            Text = " Chaos: ",
            ColorScheme = UiThemes.Instance.SessionHeader
        };

        _chaosLabelValue = new Label
        {
            X = Pos.Right(_chaosLabelKey),
            Y = 0,
            Text = "",
            ColorScheme = UiThemes.Instance.SessionHeaderValue
        };

        // Separator
        var sep3 = new Label
        {
            X = Pos.Right(_chaosLabelValue),
            Y = 0,
            Text = " |",
            ColorScheme = UiThemes.Instance.SessionHeader
        };

        // Characters label
        _charactersLabelKey = new Label
        {
            X = Pos.Right(sep3),
            Y = 0,
            Text = " Characters: ",
            ColorScheme = UiThemes.Instance.SessionHeader
        };

        _charactersLabelValue = new Label
        {
            X = Pos.Right(_charactersLabelKey),
            Y = 0,
            Text = "",
            ColorScheme = UiThemes.Instance.SessionHeaderValue
        };

        // Separator
        var sep4 = new Label
        {
            X = Pos.Right(_charactersLabelValue),
            Y = 0,
            Text = " |",
            ColorScheme = UiThemes.Instance.SessionHeader
        };

        // Threads label
        _threadsLabelKey = new Label
        {
            X = Pos.Right(sep4),
            Y = 0,
            Text = " Threads: ",
            ColorScheme = UiThemes.Instance.SessionHeader
        };

        _threadsLabelValue = new Label
        {
            X = Pos.Right(_threadsLabelKey),
            Y = 0,
            Text = "",
            ColorScheme = UiThemes.Instance.SessionHeaderValue
        };

        // Spacer to fill the rest with cyan background
        _spacer = new Label
        {
            X = Pos.Right(_threadsLabelValue),
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
            Text = "",
            ColorScheme = UiThemes.Instance.SessionHeader
        };

        Add(_appLabel, sep1, _campaignLabelKey, _campaignLabelValue, sep2,
            _chaosLabelKey, _chaosLabelValue, sep3,
            _charactersLabelKey, _charactersLabelValue, sep4,
            _threadsLabelKey, _threadsLabelValue, _spacer);

        Refresh();
    }

    public void Refresh()
    {
        var campaignName = _campaignService.CurrentCampaign?.Name ?? "No Campaign";
        var chaos = _session.Chaos;
        var characters = _stateManager.CharacterCount;
        var threads = _stateManager.ActiveThreadCount;

        _campaignLabelValue.Text = campaignName;
        _chaosLabelValue.Text = chaos.ToString();
        _chaosLabelValue.ColorScheme = UiThemes.Instance.ForChaosOnHeader(chaos);
        _charactersLabelValue.Text = characters.ToString();
        _threadsLabelValue.Text = threads.ToString();

        SetNeedsLayout();
    }
}
