using GeodeticEngine.Geodetic;
using Xunit;
using FluentAssertions;

namespace GeodeticEngine.Tests;

public class UnitTest1
{
    [Fact]
    public async Task Undulation_at_Seville_should_be_correct()
    {
        const double sevilla_lat = 37.34;
        const double sevilla_lon = -5.97;

        while (!Geoid.Ready)
            await Task.Delay(100);
        
        var undulation = Geoid.GetUndulation(sevilla_lat, sevilla_lon, false);

        undulation.Should().BeApproximately(49.6599f, 0.01f);
    }
}