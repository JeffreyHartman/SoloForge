namespace SoloForge.Console.Services;

public interface ITableWordSource
{
    string GetRandomWord(string tableId);
    string GetFusionPair(string tableId1, string tableId2);
    TableInfo? FindTable(string tableId);
}
