using SoloForge.Core.Models;

namespace SoloForge.Core.Services;

public sealed class TemplateServiceRenderer : ITemplateRenderer
{
    public string ToMarkdown(LogEntry entry) => TemplateService.Instance.ToMarkdown(entry);
}
