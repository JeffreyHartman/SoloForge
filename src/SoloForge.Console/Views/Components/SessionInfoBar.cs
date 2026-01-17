using Terminal.Gui;
using SoloForge.Console.Core;
using SoloForge.Console.Services;

namespace SoloForge.Console.Views.Components;

/// <summary>
/// Status bar displaying campaign name, chaos factor, character/thread counts.
/// </summary>
public class SessionInfoBar : View
{
    private readonly Session _session;
    private readonly AdventureStateManager _stateManager;
    private readonly CampaignService _campaignService;
    private readonly Label _label;

    public SessionInfoBar(Session session, AdventureStateManager stateManager, CampaignService campaignService)
    {
        _session = session;
        _stateManager = stateManager;
        _campaignService = campaignService;

        _label = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
            ColorScheme = new ColorScheme
            {
                Normal = new Terminal.Gui.Attribute(Color.Black, Color.Cyan)
            }
        };

        Add(_label);
        Refresh();
    }

    public void Refresh()
    {
        var campaignName = _campaignService.CurrentCampaign?.Name ?? "No Campaign";
        var chaos = _session.Chaos;
        var characters = _stateManager.CharacterCount;
        var threads = _stateManager.ActiveThreadCount;

        _label.Text = $" SoloForge | Campaign: {campaignName} | Chaos: {chaos} | Characters: {characters} | Threads: {threads} ";
        SetNeedsLayout();
    }
}
