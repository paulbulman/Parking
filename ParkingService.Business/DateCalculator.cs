﻿namespace ParkingService.Business
{
    using System.Collections.Generic;
    using System.Linq;
    using Data;
    using NodaTime;

    public class DateCalculator
    {
        private static readonly DateTimeZone LondonTimeZone = DateTimeZoneProviders.Tzdb["Europe/London"];

        private readonly IBankHolidayRepository bankHolidayRepository;

        public DateCalculator(IClock clock, IBankHolidayRepository bankHolidayRepository)
        {
            this.bankHolidayRepository = bankHolidayRepository;
            this.CurrentInstant = clock.GetCurrentInstant();
        }

        private Instant CurrentInstant { get; }

        public IReadOnlyCollection<LocalDate> GetActiveDates()
        {
            var currentDate = this.GetCurrentDate();

            var firstDayOfThisMonth = new LocalDate(currentDate.Year, currentDate.Month, 1);
            var firstDayOfSubsequentMonth = firstDayOfThisMonth.PlusMonths(2);
            var lastDayOfNextMonth = firstDayOfSubsequentMonth.PlusDays(-1);

            return this.DatesBetween(currentDate, lastDayOfNextMonth);
        }

        private LocalDate GetCurrentDate() => this.GetCurrentTime().Date;

        private ZonedDateTime GetCurrentTime() => this.CurrentInstant.InZone(LondonTimeZone);

        private IReadOnlyCollection<LocalDate> DatesBetween(LocalDate firstDate, LocalDate lastDate) =>
            Enumerable.Range(0, Period.Between(firstDate, lastDate, PeriodUnits.Days).Days + 1)
                .Select(firstDate.PlusDays)
                .Where(this.IsWorkingDay)
                .ToArray();

        private bool IsWorkingDay(LocalDate date) =>
            date.DayOfWeek != IsoDayOfWeek.Saturday &&
            date.DayOfWeek != IsoDayOfWeek.Sunday &&
            this.bankHolidayRepository.GetBankHolidays().All(b => b.Date != date);
    }
}
