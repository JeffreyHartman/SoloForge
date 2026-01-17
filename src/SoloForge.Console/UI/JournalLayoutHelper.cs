using Spectre.Console.Rendering;
using SoloForge.Console.Services;

namespace SoloForge.Console.UI;

public static class JournalLayoutHelper
{
    public static bool ShouldIgnoreLeftInput(JournalService journal) => journal.Focus == JournalFocus.Journal;

    public static void Refresh(JournalService journal, IRenderable left, string title, int chaos, int characters, int threads, string? campaignName, string? footer = null)
    {
        MythicUi.RenderSplitScreen(left, journal, title, chaos, characters, threads, campaignName, footer);
    }
}
