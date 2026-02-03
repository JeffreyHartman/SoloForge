namespace SoloForge.Console.Services;

public sealed class SharedRng : IRng
{
    private SharedRng() { }

    private static readonly Lazy<SharedRng> _instance = new(() => new SharedRng());
    public static SharedRng Instance => _instance.Value;

    public int Next(int minInclusive, int maxExclusive) => Random.Shared.Next(minInclusive, maxExclusive);
}
