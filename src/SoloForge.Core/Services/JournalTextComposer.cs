namespace SoloForge.Core.Services;

public static class JournalTextComposer
{
    public static string AppendMarkdown(string currentText, string markdown)
    {
        currentText ??= string.Empty;
        markdown ??= string.Empty;

        if (!string.IsNullOrWhiteSpace(currentText) && !currentText.EndsWith("\n\n\n", StringComparison.Ordinal))
        {
            currentText = currentText.TrimEnd() + "\n\n\n";
        }

        return currentText + markdown + "\n";
    }
}
