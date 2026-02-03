namespace SoloForge.Console.Services;

public interface IRng
{
    int Next(int minInclusive, int maxExclusive);
}
