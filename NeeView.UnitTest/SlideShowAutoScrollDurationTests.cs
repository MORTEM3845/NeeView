using System;
using Xunit;

namespace NeeView.UnitTest
{
    public class SlideShowAutoScrollDurationTests
    {
        [Theory]
        [InlineData(600.0, 300.0, 2.0)]
        [InlineData(1200.0, 300.0, 4.0)]
        [InlineData(3000.0, 300.0, 10.0)]
        public void CalculatesDurationFromDistanceAndPixelsPerSecond(double distance, double speed, double expectedSeconds)
        {
            var result = SlideShowAutoScrollDuration.Calculate(distance, speed, TimeSpan.FromSeconds(5));

            Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), result);
        }

        [Fact]
        public void UsesSlideshowIntervalWhenNoScrollIsNeeded()
        {
            var interval = TimeSpan.FromSeconds(5);

            var result = SlideShowAutoScrollDuration.Calculate(0.0, 300.0, interval);

            Assert.Equal(interval, result);
        }

        [Fact]
        public void RejectsNonPositiveSpeedWhenScrollIsNeeded()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SlideShowAutoScrollDuration.Calculate(100.0, 0.0, TimeSpan.FromSeconds(5)));
        }
    }
}
