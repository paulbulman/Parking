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
