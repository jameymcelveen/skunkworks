namespace Cartographer.Core.Grid;

/// <summary>
/// EPSG:3857 Web Mercator projection helpers (sphere radius 6378137 m).
/// </summary>
public static class WebMercator
{
    /// <summary>WGS84 / Web Mercator sphere radius in meters.</summary>
    public const double EarthRadiusMeters = 6_378_137.0;

    /// <summary>Maximum valid latitude for Web Mercator (degrees).</summary>
    public const double MaxLatitude = 85.05112878;

    private const double OriginShift = Math.PI * EarthRadiusMeters;

    /// <summary>Converts geographic degrees to Web Mercator meters.</summary>
    public static MercatorPoint FromLatLng(LatLng latLng)
    {
        var lat = Math.Clamp(latLng.Lat, -MaxLatitude, MaxLatitude);
        var lng = latLng.Lng;

        // Exact zeros avoid floor-dividing a tiny negative into the previous cell.
        var x = lng == 0.0 ? 0.0 : EarthRadiusMeters * DegToRad(lng);
        double y;
        if (lat == 0.0)
        {
            y = 0.0;
        }
        else
        {
            var sin = Math.Sin(DegToRad(lat));
            y = EarthRadiusMeters * 0.5 * Math.Log((1.0 + sin) / (1.0 - sin));
        }

        return new MercatorPoint(x, y);
    }

    /// <summary>Converts Web Mercator meters to geographic degrees.</summary>
    public static LatLng ToLatLng(MercatorPoint point)
    {
        var lng = RadToDeg(point.X / EarthRadiusMeters);
        var lat = RadToDeg(2.0 * Math.Atan(Math.Exp(point.Y / EarthRadiusMeters)) - Math.PI / 2.0);
        return new LatLng(lat, lng);
    }

    /// <summary>World extent in meters along one axis from -OriginShift to +OriginShift.</summary>
    public static double OriginShiftMeters => OriginShift;

    private static double DegToRad(double degrees) => degrees * Math.PI / 180.0;

    private static double RadToDeg(double radians) => radians * 180.0 / Math.PI;
}
