namespace SoloForge.Console.Services;

public sealed class TableServiceWordSource : ITableWordSource
{
    public string GetRandomWord(string tableId) => TableService.Instance.GetRandomWord(tableId);

    public string GetFusionPair(string tableId1, string tableId2) => TableService.Instance.GetFusionPair(tableId1, tableId2);

    public TableInfo? FindTable(string tableId) => TableService.Instance.FindTable(tableId);
}
