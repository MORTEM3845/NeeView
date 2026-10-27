using System.Windows;

namespace NeeView.UnitTest
{
    public class DetailToolTipPlacementTests
    {
        private static readonly Rect WorkArea = new(0, 0, 1920, 1080);

        [Fact]
        public void PrefersRightOfCursorWhenThereIsRoom()
        {
            var bounds = DetailToolTipPlacement.CalculateBounds(new Point(400, 250), new Size(300, 100), WorkArea);

            Assert.Equal(412, bounds.X);
            Assert.Equal(250, bounds.Y);
            Assert.Equal(300, bounds.Width);
            Assert.Equal(100, bounds.Height);
        }

        [Fact]
        public void FallsBackToLeftNearRightEdge()
        {
            var bounds = DetailToolTipPlacement.CalculateBounds(new Point(1850, 250), new Size(300, 100), WorkArea);

            Assert.Equal(1538, bounds.X);
            Assert.Equal(250, bounds.Y);
            Assert.True(bounds.Right <= WorkArea.Right);
        }

        [Theory]
        [InlineData(-20, 0)]
        [InlineData(1070, 980)]
        public void ClampsVerticallyAtTopAndBottomEdges(double cursorY, double expectedY)
        {
            var bounds = DetailToolTipPlacement.CalculateBounds(new Point(400, cursorY), new Size(300, 100), WorkArea);

            Assert.Equal(expectedY, bounds.Y);
            Assert.True(bounds.Top >= WorkArea.Top);
            Assert.True(bounds.Bottom <= WorkArea.Bottom);
        }

        [Fact]
        public void KeepsLongFilenameTooltipWithinWorkArea()
        {
            // A long filename can produce a tooltip wider than the available space to the cursor's right.
            var bounds = DetailToolTipPlacement.CalculateBounds(new Point(1750, 500), new Size(1200, 180), WorkArea);

            Assert.Equal(538, bounds.X);
            Assert.Equal(1200, bounds.Width);
            Assert.True(bounds.Left >= WorkArea.Left);
            Assert.True(bounds.Right <= WorkArea.Right);
        }
    }
}
