namespace Cartographer.Core.Grid;

/// <summary>
/// Integer cell coordinate on a fixed-size Web Mercator grid.
/// </summary>
public readonly record struct CellIndex(long X, long Y);
