using ServicePilot.Application.Common;

namespace ServicePilot.UnitTests.Common;

public sealed class PageResultTests
{
    [Fact]
    public void Normalize_BoundsExtremePageAndCalculatesSkipWithoutOverflow()
    {
        (int page, int pageSize) = PageResult<object>.Normalize(
            int.MaxValue,
            100);

        Assert.Equal(PageResult<object>.MaxPage, page);
        Assert.Equal(100, pageSize);
        Assert.Equal(99_999_900, PageResult<object>.CalculateSkip(page, pageSize));
    }
}
