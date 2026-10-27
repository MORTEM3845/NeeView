namespace NeeView.UnitTest
{
    public class PageSelectDialogModelTests
    {
        [Theory]
        [InlineData("１２３", 123)]
        [InlineData("1２3", 123)]
        [InlineData("-１２", -12)]
        [InlineData(" １２ ", 12)]
        public void TryParseValue_AcceptsIntegerWithFullWidthDigits(string text, int expected)
        {
            Assert.True(PageSelectDialogModel.TryParseValue(text, out var actual));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("")]
        [InlineData("１２x３")]
        [InlineData("1.２")]
        [InlineData("２ ３")]
        [InlineData("２１４７４８３６４８")]
        [InlineData("-２１４７４８３６４９")]
        public void TryParseValue_RejectsInvalidOrOutOfRangeInput(string text)
        {
            Assert.False(PageSelectDialogModel.TryParseValue(text, out _));
        }

        [Theory]
        [InlineData(-10, 1)]
        [InlineData(10, 10)]
        [InlineData(0, 5)]
        public void AddValue_ClampsToDialogBounds(int delta, int expected)
        {
            var model = new PageSelectDialogModel(5, 1, 10);

            model.AddValue(delta);

            Assert.Equal(expected, model.Value);
        }
    }
}
