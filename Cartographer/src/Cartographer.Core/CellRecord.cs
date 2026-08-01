using Cartographer.Core.Grid;

namespace Cartographer.Core;

/// <summary>
/// Stored cell row for a grid.
/// </summary>
public sealed record CellRecord(
    string GridId,
    CellIndex Index,
    TerrainClass Class,
    TerrainClass? SecondaryClass,
    float Confidence,
    DateTimeOffset SampledAt,
    DateTimeOffset ExpiresAt);
