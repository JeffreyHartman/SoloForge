using Terminal.Gui;
using SoloForge.Console.App;
using SoloForge.Console.Core;
using SoloForge.Console.Services;

namespace SoloForge.Console.Views.Components;

/// <summary>
/// Left panel displaying campaign info: name, chaos, characters, threads, engine.
/// Uses vertical layout with color-coded values.
/// </summary>
public class CampaignInfoPanel : FrameView
{
    private readonly Session _session;
    private readonly AdventureStateManager _stateManager;
    private readonly CampaignService _campaignService;

    private readonly Label _campaignValue;
    private readonly Label _chaosValue;
    private readonly Label _engineValue;
    private readonly Label _charactersValue;
    private readonly Label _threadsValue;

    public CampaignInfoPanel(Session session, AdventureStateManager stateManager, CampaignService campaignService)
    {
        _session = session;
        _stateManager = stateManager;
        _campaignService = campaignService;

        Title = "Campaign";
        BorderStyle = LineStyle.Double;
        ColorScheme = UiThemes.Instance.ActiveDefault;

        var y = 0;

        // Campaign name
        Add(new Label
        {
            X = 1,
            Y = y,
            Text = "Name:",
            ColorScheme = UiThemes.Instance.ActiveMuted
        });
        y++;

        _campaignValue = new Label
        {
            X = 1,
            Y = y,
            Width = Dim.Fill(1),
            ColorScheme = UiThemes.Instance.ActiveAccent
        };
        Add(_campaignValue);
        y += 2;

        // Chaos factor
        Add(new Label
        {
            X = 1,
            Y = y,
            Text = "Chaos:",
            ColorScheme = UiThemes.Instance.ActiveMuted
        });

        _chaosValue = new Label
        {
            X = 9,
            Y = y,
            Width = 2,
            ColorScheme = UiThemes.Instance.ActiveWarning
        };
        Add(_chaosValue);
        y += 2;

        // Engine
        Add(new Label
        {
            X = 1,
            Y = y,
            Text = "Engine:",
            ColorScheme = UiThemes.Instance.ActiveMuted
        });

        _engineValue = new Label
        {
            X = 10,
            Y = y,
            Width = Dim.Fill(1),
            ColorScheme = UiThemes.Instance.ActivePrimary
        };
        Add(_engineValue);
        y += 2;

        // Separator
        Add(new Label
        {
            X = 1,
            Y = y,
            Width = Dim.Fill(1),
            Text = new string('\u2500', 15), // horizontal line
            ColorScheme = UiThemes.Instance.ActiveMuted
        });
        y++;

        // Characters count
        Add(new Label
        {
            X = 1,
            Y = y,
            Text = "Characters:",
            ColorScheme = UiThemes.Instance.ActiveMuted
        });

        _charactersValue = new Label
        {
            X = 13,
            Y = y,
            Width = 3,
            ColorScheme = UiThemes.Instance.ActiveDefault
        };
        Add(_charactersValue);
        y++;

        // Threads count
        Add(new Label
        {
            X = 1,
            Y = y,
            Text = "Threads:",
            ColorScheme = UiThemes.Instance.ActiveMuted
        });

        _threadsValue = new Label
        {
            X = 13,
            Y = y,
            Width = 3,
            ColorScheme = UiThemes.Instance.ActiveDefault
        };
        Add(_threadsValue);

        Refresh();
    }

    public void Refresh()
    {
        var campaign = _campaignService.CurrentCampaign;
        var chaos = _session.Chaos;

        // Update ColorScheme for the panel itself (for theme changes)
        ColorScheme = UiThemes.Instance.ActiveDefault;

        _campaignValue.Text = campaign?.Name ?? "No Campaign";
        _campaignValue.ColorScheme = UiThemes.Instance.ActiveAccent;

        _chaosValue.Text = chaos.ToString();
        _chaosValue.ColorScheme = UiThemes.Instance.ForChaos(chaos);

        _engineValue.Text = _session.Engine;
        _engineValue.ColorScheme = UiThemes.Instance.ActivePrimary;

        _charactersValue.Text = _stateManager.CharacterCount.ToString();
        _charactersValue.ColorScheme = UiThemes.Instance.ActiveDefault;

        _threadsValue.Text = _stateManager.ActiveThreadCount.ToString();
        _threadsValue.ColorScheme = UiThemes.Instance.ActiveDefault;

        // Update all static labels' color schemes
        foreach (var child in Subviews)
        {
            if (child is Label label && label != _campaignValue && label != _chaosValue &&
                label != _engineValue && label != _charactersValue && label != _threadsValue)
            {
                label.ColorScheme = UiThemes.Instance.ActiveMuted;
            }
        }

        SetNeedsLayout();
    }
}
