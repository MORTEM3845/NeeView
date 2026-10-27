using NeeView;

namespace NeeView.UnitTest;

public class PageSelectDialogModelTests
{
    [Theory]
    [InlineData("123", 123)]
    [InlineData("１２３", 123)]
    [InlineData("1２3", 123)]
    [InlineData("١٢٣", 123)]
    [InlineData("+１２３", 123)]
    [InlineData(" -１２３ ", -123)]
    [InlineData("0", 0)]
    [InlineData("11", 11)]
    public void TryParseValue_AcceptsAsciiAndUnicodeDecimalDigits(string text, int expected)
    {
        Assert.True(PageSelectDialogModel.TryParseValue(text, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("１２a３")]
    [InlineData("１２ ３")]
    [InlineData("−１２")]
    public void TryParseValue_RejectsNonDigits(string text)
    {
        Assert.False(PageSelectDialogModel.TryParseValue(text, out _));
    }

    [Theory]
    [InlineData("2147483647", true, int.MaxValue)]
    [InlineData("-2147483648", true, int.MinValue)]
    [InlineData("2147483648", false, 0)]
    [InlineData("-2147483649", false, 0)]
    [InlineData("９９９９９９９９９９", false, 0)]
    public void TryParseValue_HandlesIntegerOverflow(string text, bool expectedResult, int expectedValue)
    {
        Assert.Equal(expectedResult, PageSelectDialogModel.TryParseValue(text, out var value));
        Assert.Equal(expectedValue, value);
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
