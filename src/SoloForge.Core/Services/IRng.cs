namespace SoloForge.Core.Services;

public interface IRng
{
    int Next(int minInclusive, int maxExclusive);
}
