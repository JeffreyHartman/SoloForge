using SoloForge.Console.Models;

namespace SoloForge.Console.Services;

public interface ITemplateRenderer
{
    string ToMarkdown(LogEntry entry);
}
