namespace Parking.Business.UnitTests;

using System.Linq;
using Data;
using Model;
using Moq;
using NodaTime;
using NodaTime.Testing;
using NodaTime.Testing.Extensions;
using TestHelpers;
using Xunit;

public static class DateCalculatorTests
{
    private static readonly DateTimeZone LondonTimeZone = DateTimeZoneProviders.Tzdb["Europe/London"];

    [Fact]
    public static void InitialInstant_returns_same_instant_for_successive_calls()
    {
        var initialInstant = 30.December(2020).At(11, 58, 33).Utc();
        var autoAdvance = 10.Seconds();
            
        var fakeClock = new FakeClock(initialInstant, autoAdvance);

        var dateCalculator = new DateCalculator(fakeClock, Mock.Of<IBankHolidayRepository>());

        var firstResult = dateCalculator.InitialInstant;
        var subsequentResult = dateCalculator.InitialInstant;

        Assert.Equal(firstResult, subsequentResult);
    }

    [Fact]
    public static void GetActiveDates_returns_current_date_first()
    {
        var currentDate = 28.August(2020);
        var result = CreateDateCalculator(currentDate).GetActiveDates();
        Assert.Equal(currentDate, result.First());
    }

    [Fact]
    public static void GetActiveDates_returns_last_day_of_next_month_last()
    {
        var result = CreateDateCalculator(28.August(2020)).GetActiveDates();
        Assert.Equal(30.September(2020), result.Last());
    }

    [Fact]
    public static void GetActiveDates_excludes_weekends()
    {
        var result = CreateDateCalculator(28.August(2020)).GetActiveDates();
        Assert.DoesNotContain(29.August(2020), result);
        Assert.DoesNotContain(30.August(2020), result);
    }

    [Fact]
    public static void GetActiveDates_excludes_bank_holidays()
    {
        var bankHoliday = 31.August(2020);
        var result = CreateDateCalculator(28.August(2020), bankHoliday).GetActiveDates();
        Assert.DoesNotContain(bankHoliday, result);
    }

    [Fact]
    public static void GetActiveDates_uses_London_time_zone()
    {
        var winterInstant = 31.January(2020).At(23, 0, 0).Utc();
        var winterResult = CreateDateCalculator(winterInstant).GetActiveDates();
        Assert.Equal(31.January(2020), winterResult.First());

        var summerInstant = 30.June(2020).At(23, 0, 0).Utc();
        var summerResult = CreateDateCalculator(summerInstant).GetActiveDates();
        Assert.Equal(1.July(2020), summerResult.First());
    }

    [Fact]
    public static void GetShortLeadTimeAllocationDates_returns_current_working_day_if_called_before_11_am()
    {
        var instant = 4.September(2020).At(10, 59, 59).InZoneStrictly(LondonTimeZone).ToInstant();
            
        var result = CreateDateCalculator(instant).GetShortLeadTimeAllocationDates();

        Assert.Single(result);
        Assert.Equal(4.September(2020), result.Single());
    }

    [Fact]
    public static void GetShortLeadTimeAllocationDates_returns_current_and_next_working_days_if_called_after_11_am()
    {
        var instant = 4.September(2020).At(11, 0, 0).InZoneStrictly(LondonTimeZone).ToInstant(); 

        var result = CreateDateCalculator(instant).GetShortLeadTimeAllocationDates();

        Assert.Equal(2, result.Count);
        Assert.Equal(4.September(2020), result.First());
        Assert.Equal(7.September(2020), result.Last());
    }

    [Fact]
    public static void GetShortLeadTimeAllocationDates_uses_London_time_zone()
    {
        var winterInstant = 31.January(2020).At(10, 0, 0).Utc();
        var winterResult = CreateDateCalculator(winterInstant).GetShortLeadTimeAllocationDates();
        Assert.Equal(31.January(2020), winterResult.Last());

        var summerInstant = 30.June(2020).At(10, 0, 0).Utc();
        var summerResult = CreateDateCalculator(summerInstant).GetShortLeadTimeAllocationDates();
        Assert.Equal(1.July(2020), summerResult.Last());
    }

    [Fact]
    public static void GetShortLeadTimeAllocationDates_returns_next_working_day_if_called_at_weekend()
    {
        var instant = 5.September(2020).At(11, 0, 0).InZoneStrictly(LondonTimeZone).ToInstant();

        var result = CreateDateCalculator(instant).GetShortLeadTimeAllocationDates();

        Assert.Single(result);
        Assert.Equal(7.September(2020), result.Single());
    }

    [Fact]
    public static void GetShortLeadTimeAllocationDates_returns_next_working_day_if_called_on_bank_holiday()
    {
        var bankHoliday = 31.August(2020);
        var instant = bankHoliday.At(11, 0, 0).InZoneStrictly(LondonTimeZone).ToInstant();

        var result = CreateDateCalculator(instant, bankHoliday).GetShortLeadTimeAllocationDates();

        Assert.Single(result);
        Assert.Equal(1.September(2020), result.Single());
    }

    [Fact]
    public static void GetLongLeadTimeAllocationDates_returns_date_after_short_lead_time_period_first()
    {
        var result = CreateDateCalculator(7.September(2020)).GetLongLeadTimeAllocationDates();
        Assert.Equal(8.September(2020), result.First());
    }

    [Fact]
    public static void GetLongLeadTimeAllocationDates_returns_date_at_end_of_next_week_last()
    {
        var result = CreateDateCalculator(7.September(2020)).GetLongLeadTimeAllocationDates();
        Assert.Equal(18.September(2020), result.Last());
    }

    [Fact]
    public static void GetLongLeadTimeAllocationDates_rolls_over_to_new_week_on_Thursdays()
    {
        // On a Wednesday, the last date should be the Friday 9 days later.
        var wednesdayResult = CreateDateCalculator(9.September(2020)).GetLongLeadTimeAllocationDates();
        Assert.Equal(10.September(2020), wednesdayResult.First());
        Assert.Equal(18.September(2020), wednesdayResult.Last());

        // On a Thursday, the last date should be the Friday 15 days later.
        var thursdayResult = CreateDateCalculator(10.September(2020)).GetLongLeadTimeAllocationDates();
        Assert.Equal(11.September(2020), thursdayResult.First());
        Assert.Equal(25.September(2020), thursdayResult.Last());
    }

    [Fact]
    public static void GetLongLeadTimeAllocationDates_uses_London_time_zone()
    {
        // This is still Wednesday local time, so should only includes the current and next weeks.
        var winterInstant = 1.January(2020).At(23, 0, 0).Utc();
        var winterResult = CreateDateCalculator(winterInstant).GetLongLeadTimeAllocationDates();
        Assert.Equal(10.January(2020), winterResult.Last());

        // This is Thursday local time, so should include the current, next and one subsequent week.
        var summerInstant = 3.June(2020).At(23, 0, 0).Utc();
        var summerResult = CreateDateCalculator(summerInstant).GetLongLeadTimeAllocationDates();
        Assert.Equal(19.June(2020), summerResult.Last());
    }

    [Fact]
    public static void GetWeeklyNotificationDates_returns_ordered_dates_from_next_week()
    {
        var result = CreateDateCalculator(7.September(2020)).GetWeeklyNotificationDates();

        Assert.Equal(5, result.Count);

        Assert.Equal(result.OrderBy(d => d), result);

        Assert.Equal(14.September(2020), result.First());
        Assert.Equal(18.September(2020), result.Last());
    }

    [Fact]
    public static void GetWeeklyNotificationDates_rolls_over_to_new_week_on_Thursdays()
    {
        // On a Wednesday, the last date should be the Friday 9 days later.
        var wednesdayResult = CreateDateCalculator(9.September(2020)).GetWeeklyNotificationDates();
        Assert.Equal(14.September(2020), wednesdayResult.First());
        Assert.Equal(18.September(2020), wednesdayResult.Last());

        // On a Thursday, the last date should be the Friday 15 days later.
        var thursdayResult = CreateDateCalculator(10.September(2020)).GetWeeklyNotificationDates();
        Assert.Equal(21.September(2020), thursdayResult.First());
        Assert.Equal(25.September(2020), thursdayResult.Last());
    }

    [Fact]
    public static void GetWeeklyNotificationDates_uses_London_time_zone()
    {
        // This is still Wednesday local time, so should return the next week.
        var winterInstant = 1.January(2020).At(23, 0, 0).Utc();
        var winterResult = CreateDateCalculator(winterInstant).GetWeeklyNotificationDates();
        Assert.Equal(6.January(2020), winterResult.First());
        Assert.Equal(10.January(2020), winterResult.Last());

        // This is Thursday local time, so should return the subsequent week.
        var summerInstant = 3.June(2020).At(23, 0, 0).Utc();
        var summerResult = CreateDateCalculator(summerInstant).GetWeeklyNotificationDates();
        Assert.Equal(15.June(2020), summerResult.First());
        Assert.Equal(19.June(2020), summerResult.Last());
    }

    [Fact]
    public static void GetNextWeeklyNotificationDates_returns_ordered_dates_from_next_but_one_week()
    {
        var result = CreateDateCalculator(7.September(2020)).GetNextWeeklyNotificationDates();

        Assert.Equal(5, result.Count);

        Assert.Equal(result.OrderBy(d => d), result);

        Assert.Equal(21.September(2020), result.First());
        Assert.Equal(25.September(2020), result.Last());
    }

    [Fact]
    public static void GetNextWeeklyNotificationDates_rolls_over_to_new_week_on_Thursdays()
    {
        // On a Wednesday, the last date should be the Friday 16 days later.
        var wednesdayResult = CreateDateCalculator(9.September(2020)).GetNextWeeklyNotificationDates();
        Assert.Equal(21.September(2020), wednesdayResult.First());
        Assert.Equal(25.September(2020), wednesdayResult.Last());

        // On a Thursday, the last date should be the Friday 22 days later.
        var thursdayResult = CreateDateCalculator(10.September(2020)).GetNextWeeklyNotificationDates();
        Assert.Equal(28.September(2020), thursdayResult.First());
        Assert.Equal(2.October(2020), thursdayResult.Last());
    }

    [Fact]
    public static void GetNextWeeklyNotificationDates_uses_London_time_zone()
    {
        // This is still Wednesday local time, so should return the next-but-one week.
        var winterInstant = 1.January(2020).At(23, 0, 0).Utc();
        var winterResult = CreateDateCalculator(winterInstant).GetNextWeeklyNotificationDates();
        Assert.Equal(13.January(2020), winterResult.First());
        Assert.Equal(17.January(2020), winterResult.Last());

        // This is Thursday local time, so should return the subsequent week.
        var summerInstant = 3.June(2020).At(23, 0, 0).Utc();
        var summerResult = CreateDateCalculator(summerInstant).GetNextWeeklyNotificationDates();
        Assert.Equal(22.June(2020), summerResult.First());
        Assert.Equal(26.June(2020), summerResult.Last());
    }

    [Fact]
    public static void GetNextWorkingDate_returns_next_working_date_when_called_on_working_date()
    {
        var instant = 23.December(2020).At(10, 0, 0).Utc();
        var result = CreateDateCalculator(instant).GetNextWorkingDate();
        Assert.Equal(24.December(2020), result);
    }

    [Fact]
    public static void GetNextWorkingDate_returns_next_working_date_when_called_at_weekend()
    {
        var instant = 19.December(2020).At(10, 0, 0).Utc();
        var result = CreateDateCalculator(instant).GetNextWorkingDate();
        Assert.Equal(21.December(2020), result);
    }

    [Fact]
    public static void GetNextWorkingDate_returns_next_working_date_when_called_on_bank_holiday()
    {
        var bankHolidays = new[] {25.December(2020), 28.December(2020)};
            
        var instant = 25.December(2020).At(10, 0, 0).Utc();
        var result = CreateDateCalculator(instant, bankHolidays).GetNextWorkingDate();
        Assert.Equal(29.December(2020), result);
    }

    [Fact]
    public static void GetNextWorkingDate_uses_London_time_zone()
    {
        // This is still Wednesday local time, so should return the Thursday.
        var winterInstant = 1.January(2020).At(23, 0, 0).Utc();
        var winterResult = CreateDateCalculator(winterInstant).GetNextWorkingDate();
        Assert.Equal(2.January(2020), winterResult);

        // This is Thursday local time, so should return the Friday.
        var summerInstant = 3.June(2020).At(23, 0, 0).Utc();
        var summerResult = CreateDateCalculator(summerInstant).GetNextWorkingDate();
        Assert.Equal(5.June(2020), summerResult);
    }

    [Fact]
    public static void ScheduleIsDue_returns_true_when_due_time_is_in_the_past()
    {
        var currentInstant = 12.January(2021).At(16, 48, 16).Utc();

        var schedule = new Schedule(ScheduledTaskType.DailyNotification, currentInstant.Plus(-1.Seconds()));

        var result = CreateDateCalculator(currentInstant).ScheduleIsDue(schedule);

        Assert.True(result);
    }

    [Fact]
    public static void ScheduleIsDue_returns_false_when_due_time_is_in_the_future()
    {
        var currentInstant = 12.January(2021).At(16, 48, 16).Utc();

        var schedule = new Schedule(ScheduledTaskType.DailyNotification, currentInstant.Plus(1.Seconds()));

        var result = CreateDateCalculator(currentInstant).ScheduleIsDue(schedule);

        Assert.False(result);
    }

    public static DateCalculator CreateDateCalculator(Instant instant, params LocalDate[] bankHolidayDates)
    {
        var mockBankHolidayRepository = new Mock<IBankHolidayRepository>();
        mockBankHolidayRepository
            .Setup(r => r.GetBankHolidays())
            .Returns(bankHolidayDates.Select(d => new BankHoliday(d)).ToArray());

        return new DateCalculator(new FakeClock(instant), mockBankHolidayRepository.Object);
    }

    private static DateCalculator CreateDateCalculator(LocalDate londonDate, params LocalDate[] bankHolidayDates)
    {
        var londonMidnight = londonDate.AtMidnight().InZoneStrictly(LondonTimeZone).ToInstant();

        return CreateDateCalculator(londonMidnight, bankHolidayDates);
    }
}