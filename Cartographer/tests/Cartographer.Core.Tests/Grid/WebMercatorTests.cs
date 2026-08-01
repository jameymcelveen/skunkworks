using Cartographer.Core.Grid;
using Shouldly;

namespace Cartographer.Core.Tests.Grid;

public sealed class WebMercatorTests
{
    [Fact]
    public void Origin_RoundTrips()
    {
        var m = WebMercator.FromLatLng(new LatLng(0, 0));
        m.X.ShouldBe(0, 1e-9);
        m.Y.ShouldBe(0, 1e-9);

        var back = WebMercator.ToLatLng(m);
        back.Lat.ShouldBe(0, 1e-9);
        back.Lng.ShouldBe(0, 1e-9);
    }

    [Fact]
    public void Property_RoundTripStableWithinMercatorLatRange()
    {
        var rng = new Random(11);
        for (var i = 0; i < 500; i++)
        {
            var lat = (rng.NextDouble() * 2 - 1) * WebMercator.MaxLatitude;
            var lng = rng.NextDouble() * 360.0 - 180.0;
            var original = new LatLng(lat, lng);
            var roundTripped = WebMercator.ToLatLng(WebMercator.FromLatLng(original));
            roundTripped.Lat.ShouldBe(original.Lat, 1e-9);
            roundTripped.Lng.ShouldBe(original.Lng, 1e-9);
        }
    }
}
