using Shortener.Analytics.Api.Domain;

namespace Shortener.Analytics.UnitTests.Domain;

public class DailyClickCountTests
{
    [Fact]
    public void FirstClick_ReturnsCountOfOne()
    {
        var count = DailyClickCount.FirstClick("aB3xY9z", new DateOnly(2026, 1, 1));

        Assert.Equal(1, count.Count);
    }

    [Fact]
    public void FirstClick_SetsCodeAndDate()
    {
        var date = new DateOnly(2026, 1, 1);

        var count = DailyClickCount.FirstClick("aB3xY9z", date);

        Assert.Equal("aB3xY9z", count.Code);
        Assert.Equal(date, count.Date);
    }

    [Fact]
    public void RecordOneClick_IncrementsCountByOne()
    {
        var count = DailyClickCount.FirstClick("aB3xY9z", new DateOnly(2026, 1, 1));

        count.RecordOneClick();

        Assert.Equal(2, count.Count);
    }

    [Fact]
    public void RecordOneClick_CalledRepeatedly_AccumulatesCount()
    {
        var count = DailyClickCount.FirstClick("aB3xY9z", new DateOnly(2026, 1, 1));

        count.RecordOneClick();
        count.RecordOneClick();
        count.RecordOneClick();

        Assert.Equal(4, count.Count);
    }
}
