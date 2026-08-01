using Cartographer.Core.Grid;

namespace Cartographer.Core;

/// <summary>
/// Compact row-major class grid for cheap client decoding.
/// Classes are wire strings (e.g. "water"). Null entries mean missing.
/// </summary>
public sealed record CompactCellGrid(
    string GridId,
    long OriginX,
    long OriginY,
    int Width,
    int Height,
    string?[] Classes,
    bool Pending,
    bool Stale);
