using SoloForge.Core.Models;

namespace SoloForge.Core.Services;

public interface ITemplateRenderer
{
    string ToMarkdown(LogEntry entry);
}
