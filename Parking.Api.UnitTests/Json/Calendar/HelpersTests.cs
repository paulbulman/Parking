namespace Parking.Api.UnitTests.Json.Calendar;

using System.Collections.Generic;
using System.Linq;
using NodaTime;
using NodaTime.Testing.Extensions;
using Xunit;
using static Parking.Api.Json.Calendar.Helpers;

public static class HelpersTests
{
    [Fact]
    public static void CreateCalendar_returns_visible_day_object_for_each_date_in_input_data()
    {
        var dates = new[] { 1.February(2021), 2.February(2021), 4.February(2021) };

        var data = dates.ToDictionary(d => d, d => new object());

        var result = CreateCalendar(data);

        var visibleDates = result.Weeks.SelectMany(w => w.Days).Where(d => !d.Hidden);

        Assert.Equal(dates, visibleDates.Select(d => d.LocalDate));
    }

    [Fact]
    public static void CreateCalendar_returns_hidden_day_object_for_missing_dates_in_incomplete_weeks()
    {
        var dates = new[] { 1.February(2021), 2.February(2021), 4.February(2021) };

        var data = dates.ToDictionary(d => d, d => new object());

        var result = CreateCalendar(data);

        var hiddenDates = result.Weeks.SelectMany(w => w.Days).Where(d => d.Hidden);

        var expected = new[] { 3.February(2021), 5.February(2021) };

        Assert.Equal(expected, hiddenDates.Select(d => d.LocalDate));
    }

    [Fact]
    public static void CreateCalendar_returns_days_grouped_by_week()
    {
        var dates = new[] { 3.February(2021), 8.February(2021), 12.February(2021) };

        var data = dates.ToDictionary(d => d, d => new object());

        var result = CreateCalendar(data);

        var actualWeeks = result.Weeks.ToArray();

        Assert.Equal(2, actualWeeks.Length);

        Assert.Equal(
            new[] { 3.February(2021) },
            actualWeeks[0].Days.Where(d => !d.Hidden).Select(d => d.LocalDate));
        Assert.Equal(
            new[] { 8.February(2021), 12.February(2021) },
            actualWeeks[1].Days.Where(d => !d.Hidden).Select(d => d.LocalDate));
    }

    [Fact]
    public static void CreateCalendar_returns_data_against_original_date()
    {
        var day1Data = new object();
        var day2Data = new object();

        var data = new Dictionary<LocalDate, object>
        {
            {1.February(2021), day1Data},
            {2.February(2021), day2Data}
        };

        var result = CreateCalendar(data);

        var actualDays = result.Weeks.Single().Days.ToArray();

        Assert.Equal(day1Data, actualDays.Single(d => d.LocalDate == 1.February(2021)).Data);
        Assert.Equal(day2Data, actualDays.Single(d => d.LocalDate == 2.February(2021)).Data);
    }
}