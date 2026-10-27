using Xunit;

namespace NeeView.UnitTest
{
    public class MouseWheelDeltaTests
    {
        [Theory]
        [InlineData(1, 1)]
        [InlineData(-1, -1)]
        [InlineData(0, 0)]
        [InlineData(120, 1)]
        [InlineData(-120, -1)]
        public void PageCount_UsesDeltaDirectionAsOnePage(int delta, int expected)
        {
            Assert.Equal(expected, MouseWheelDelta.PageCount(delta));
        }

        [Theory]
        [InlineData(120, 1)]
        [InlineData(-120, -1)]
        [InlineData(1, 0)]
        [InlineData(-1, 0)]
        public void NotchCount_PreservesConfiguredLineScrolling(int delta, int expected)
        {
            Assert.Equal(expected, new MouseWheelDelta().NotchCount(delta, 1));
        }
    }
}
