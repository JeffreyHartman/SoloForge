using SoloForge.Console.Models;

namespace SoloForge.Console.Services;

public sealed class TemplateServiceRenderer : ITemplateRenderer
{
    public string ToMarkdown(LogEntry entry) => TemplateService.Instance.ToMarkdown(entry);
}
