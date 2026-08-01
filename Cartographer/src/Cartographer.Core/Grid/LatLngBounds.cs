namespace Cartographer.Core.Grid;

/// <summary>
/// Inclusive geographic bounding box in WGS84 degrees.
/// </summary>
public readonly record struct LatLngBounds(double MinLng, double MinLat, double MaxLng, double MaxLat)
{
    public bool IsValid => MinLng <= MaxLng && MinLat <= MaxLat;
}
