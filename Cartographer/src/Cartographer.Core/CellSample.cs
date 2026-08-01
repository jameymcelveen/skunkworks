namespace Cartographer.Core;

/// <summary>
/// Classification result for a single cell.
/// </summary>
public sealed record CellSample(
    TerrainClass Class,
    TerrainClass? SecondaryClass,
    float Confidence);
