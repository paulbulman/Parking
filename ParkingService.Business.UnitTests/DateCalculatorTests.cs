namespace ParkingService.Business.UnitTests
{
    using System.Linq;
    using Moq;
    using NodaTime;
    using NodaTime.Testing;
    using NodaTime.Testing.Extensions;
    using Data;
    using Model;
    using Xunit;

    public class DateCalculatorTests
    {
        private static readonly DateTimeZone LondonTimeZone = DateTimeZoneProviders.Tzdb["Europe/London"];

        [Fact]
        public void GetActiveDates_returns_current_date_first()
        {
            var currentDate = 28.August(2020);
            var result = CreateDateCalculator(currentDate).GetActiveDates();
            Assert.Equal(currentDate, result.First());
        }

        [Fact]
        public void GetActiveDates_returns_last_day_of_next_month_last()
        {
            var result = CreateDateCalculator(28.August(2020)).GetActiveDates();
            Assert.Equal(30.September(2020), result.Last());
        }

        [Fact]
        public void GetActiveDates_excludes_weekends()
        {
            var result = CreateDateCalculator(28.August(2020)).GetActiveDates();
            Assert.DoesNotContain(29.August(2020), result);
            Assert.DoesNotContain(30.August(2020), result);
        }

        [Fact]
        public void GetActiveDates_excludes_bank_holidays()
        {
            var bankHoliday = 31.August(2020);
            var result = CreateDateCalculator(28.August(2020), bankHoliday).GetActiveDates();
            Assert.DoesNotContain(bankHoliday, result);
        }

        [Fact]
        public void GetActiveDates_uses_London_time_zone()
        {
            var winterInstant = 31.January(2020).At(23, 0, 0).Utc();
            var winterResult = CreateDateCalculator(winterInstant).GetActiveDates();
            Assert.Equal(31.January(2020), winterResult.First());

            var summerInstant = 30.June(2020).At(23, 0, 0).Utc();
            var summerResult = CreateDateCalculator(summerInstant).GetActiveDates();
            Assert.Equal(1.July(2020), summerResult.First());
        }

        [Fact]
        public void GetShortLeadTimeAllocationDates_returns_current_working_day_if_called_before_11_am()
        {
            var instant = 4.September(2020).At(10, 59, 59).InZoneStrictly(LondonTimeZone).ToInstant();
            
            var result = CreateDateCalculator(instant).GetShortLeadTimeAllocationDates();

            Assert.Single(result);
            Assert.Equal(4.September(2020), result.Single());
        }

        [Fact]
        public void GetShortLeadTimeAllocationDates_returns_current_and_next_working_days_if_called_after_11_am()
        {
            var instant = 4.September(2020).At(11, 0, 0).InZoneStrictly(LondonTimeZone).ToInstant(); 

            var result = CreateDateCalculator(instant).GetShortLeadTimeAllocationDates();

            Assert.Equal(2, result.Count);
            Assert.Equal(4.September(2020), result.First());
            Assert.Equal(7.September(2020), result.Last());
        }

        [Fact]
        public void GetShortLeadTimeAllocationDates_uses_London_time_zone()
        {
            var winterInstant = 31.January(2020).At(10, 0, 0).Utc();
            var winterResult = CreateDateCalculator(winterInstant).GetShortLeadTimeAllocationDates();
            Assert.Equal(31.January(2020), winterResult.Last());

            var summerInstant = 30.June(2020).At(10, 0, 0).Utc();
            var summerResult = CreateDateCalculator(summerInstant).GetShortLeadTimeAllocationDates();
            Assert.Equal(1.July(2020), summerResult.Last());
        }

        [Fact]
        public void GetShortLeadTimeAllocationDates_returns_next_working_day_if_called_at_weekend()
        {
            var instant = 5.September(2020).At(11, 0, 0).InZoneStrictly(LondonTimeZone).ToInstant();

            var result = CreateDateCalculator(instant).GetShortLeadTimeAllocationDates();

            Assert.Single(result);
            Assert.Equal(7.September(2020), result.Single());
        }

        [Fact]
        public void GetShortLeadTimeAllocationDates_returns_next_working_day_if_called_on_bank_holiday()
        {
            var bankHoliday = 31.August(2020);
            var instant = bankHoliday.At(11, 0, 0).InZoneStrictly(LondonTimeZone).ToInstant();

            var result = CreateDateCalculator(instant, bankHoliday).GetShortLeadTimeAllocationDates();

            Assert.Single(result);
            Assert.Equal(1.September(2020), result.Single());
        }

        [Fact]
        public void GetLongLeadTimeAllocationDates_returns_date_after_short_lead_time_period_first()
        {
            var result = CreateDateCalculator(7.September(2020)).GetLongLeadTimeAllocationDates();
            Assert.Equal(8.September(2020), result.First());
        }

        [Fact]
        public void GetLongLeadTimeAllocationDates_returns_date_at_end_of_next_week_last()
        {
            var result = CreateDateCalculator(7.September(2020)).GetLongLeadTimeAllocationDates();
            Assert.Equal(18.September(2020), result.Last());
        }

        [Fact]
        public void GetLongLeadTimeAllocationDates_rolls_over_to_new_week_on_Thursdays()
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
        public void GetLongLeadTimeAllocationDates_uses_London_time_zone()
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

        private static DateCalculator CreateDateCalculator(LocalDate londonDate, params LocalDate[] bankHolidayDates)
        {
            var londonMidnight = londonDate.AtMidnight().InZoneStrictly(LondonTimeZone).ToInstant();

            return CreateDateCalculator(londonMidnight, bankHolidayDates);
        }

        private static DateCalculator CreateDateCalculator(Instant instant, params LocalDate[] bankHolidayDates)
        {
            var mockBankHolidayRepository = new Mock<IBankHolidayRepository>();
            mockBankHolidayRepository
                .Setup(r => r.GetBankHolidays())
                .Returns(bankHolidayDates.Select(d => new BankHoliday(d)).ToArray());

            return new DateCalculator(new FakeClock(instant), mockBankHolidayRepository.Object);
        }
    }
}
