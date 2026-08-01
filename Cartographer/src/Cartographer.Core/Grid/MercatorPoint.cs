namespace Cartographer.Core.Grid;

/// <summary>
/// Point in EPSG:3857 Web Mercator meters.
/// </summary>
public readonly record struct MercatorPoint(double X, double Y);
