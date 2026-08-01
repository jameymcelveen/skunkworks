namespace Cartographer.Core.Grid;

/// <summary>
/// WGS84 geographic coordinate in degrees.
/// </summary>
public readonly record struct LatLng(double Lat, double Lng);
