namespace Parking.Business.UnitTests;

using System.Linq;
using Model;
using NodaTime;
using NodaTime.Testing.Extensions;
using Xunit;

public static class ExtensionMethodsTests
{
    [Fact]
    public static void StartOfWeek_returns_same_day_when_called_with_Monday()
    {
        var localDate = 15.February(2021);

        var actual = localDate.StartOfWeek();

        var expected = 15.February(2021);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(19)]
    [InlineData(20)]
    [InlineData(21)]
    public static void StartOfWeek_returns_previous_Monday_when_called_with_other_day(int day)
    {
        var localDate = day.February(2021);

        var actual = localDate.StartOfWeek();

        var expected = 15.February(2021);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public static void ToDateInterval_returns_interval_of_single_LocalDate()
    {
        var localDate = 26.August(2022);

        var actual = localDate.ToDateInterval();

        Assert.Equal(localDate, actual.Start);
        Assert.Equal(localDate, actual.End);
    }

    [Fact]
    public static void YearMonths_returns_single_YearMonth_when_Interval_is_within_single_month()
    {
        var dateInterval = new DateInterval(17.February(2021), 17.February(2021));

        var actual = dateInterval.YearMonths();

        var expected = new[] { new YearMonth(2021, 2) };

        Assert.Equal(expected, actual);
    }

    [Fact]
    public static void YearMonths_returns_multiple_YearMonths_when_Interval_spans_multiple_months()
    {
        var dateInterval = new DateInterval(31.January(2021), 1.February(2021));

        var actual = dateInterval.YearMonths();

        var expected = new[] { new YearMonth(2021, 1), new YearMonth(2021, 2) };

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(2018, 11, 7, "Wed 07 Nov")]
    [InlineData(2019, 3, 2, "Sat 02 Mar")]
    public static void ToEmailDisplayString_formats_LocalDate(
        int year,
        int month,
        int day,
        string expectedResult)
    {
        var localDate = new LocalDate(year, month, day);

        var actual = localDate.ToEmailDisplayString();

        Assert.Equal(expectedResult, actual);
    }

    [Theory]
    [InlineData(2018, 11, 6, 2019, 1, 2, "Tue 06 Nov - Wed 02 Jan")]
    [InlineData(2019, 4, 3, 2019, 4, 3, "Wed 03 Apr - Wed 03 Apr")]
    public static void ToEmailDisplayString_formats_DateInterval(
        int startYear,
        int startMonth,
        int startDay,
        int endYear,
        int endMonth,
        int endDay,
        string expectedResult)
    {
        var dateInterval = new DateInterval(
            new LocalDate(startYear, startMonth, startDay),
            new LocalDate(endYear, endMonth, endDay));

        Assert.Equal(expectedResult, dateInterval.ToEmailDisplayString());
        Assert.Equal(expectedResult, dateInterval.Reverse().ToEmailDisplayString());
    }

    [Fact]
    public static void ToDateInterval_returns_DateInterval_with_Start_and_End_set_to_earliest_and_latest_LocalDates()
    {
        var localDateCollection = new[]
        {
            26.August(2022),
            27.August(2022),
            22.August(2022),
            25.August(2022)
        };

        var actual = localDateCollection.ToDateInterval();

        Assert.Equal(22.August(2022), actual.Start);
        Assert.Equal(27.August(2022), actual.End);
    }

    [Theory]
    [InlineData(RequestStatus.Allocated, true)]
    [InlineData(RequestStatus.Cancelled, false)]
    [InlineData(RequestStatus.HardInterrupted, true)]
    [InlineData(RequestStatus.Interrupted, true)]
    [InlineData(RequestStatus.Pending, true)]
    [InlineData(RequestStatus.SoftInterrupted, true)]
    public static void IsRequested_returns_true_for_non_cancelled_statuses(RequestStatus requestStatus, bool expectedResult)
    {
        var actual = requestStatus.IsRequested();

        Assert.Equal(expectedResult, actual);
    }

    [Theory]
    [InlineData(RequestStatus.Allocated, false)]
    [InlineData(RequestStatus.Cancelled, false)]
    [InlineData(RequestStatus.HardInterrupted, false)]
    [InlineData(RequestStatus.Interrupted, true)]
    [InlineData(RequestStatus.Pending, false)]
    [InlineData(RequestStatus.SoftInterrupted, true)]
    public static void IsAllocatable_returns_true_for_interrupted_and_soft_interrupted_statuses(RequestStatus requestStatus, bool expectedResult)
    {
        var actual = requestStatus.IsAllocatable();

        Assert.Equal(expectedResult, actual);
    }

    [Theory]
    [InlineData(RequestStatus.Allocated, false)]
    [InlineData(RequestStatus.Cancelled, false)]
    [InlineData(RequestStatus.HardInterrupted, true)]
    [InlineData(RequestStatus.Interrupted, true)]
    [InlineData(RequestStatus.Pending, false)]
    [InlineData(RequestStatus.SoftInterrupted, true)]
    public static void IsInterrupted_returns_true_for_interrupted_statuses(RequestStatus requestStatus, bool expectedResult)
    {
        var actual = requestStatus.IsInterrupted();

        Assert.Equal(expectedResult, actual);
    }
}