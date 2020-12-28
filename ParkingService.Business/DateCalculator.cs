namespace ParkingService.Business
{
    using System.Collections.Generic;
    using System.Linq;
    using Data;
    using NodaTime;

    public interface IDateCalculator
    {
        IReadOnlyCollection<LocalDate> GetActiveDates();

        IReadOnlyCollection<LocalDate> GetShortLeadTimeAllocationDates();

        IReadOnlyCollection<LocalDate> GetLongLeadTimeAllocationDates();

        IReadOnlyCollection<LocalDate> GetWeeklyNotificationDates();

        LocalDate GetNextWorkingDate();
    }

    public class DateCalculator : IDateCalculator
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

            var lastDayOfNextMonth = currentDate
                .With(DateAdjusters.StartOfMonth)
                .PlusMonths(1)
                .With(DateAdjusters.EndOfMonth);

            return this.WorkingDatesBetween(currentDate, lastDayOfNextMonth);
        }

        public IReadOnlyCollection<LocalDate> GetShortLeadTimeAllocationDates()
        {
            var currentTime = this.GetCurrentTime();

            var firstDate = this.GetNextWorkingDayIncluding(currentTime.Date);

            var lastDate = firstDate == currentTime.Date && currentTime.Hour >= 11 ?
                this.GetNextWorkingDayStrictlyAfter(firstDate) :
                firstDate;

            return new[] { firstDate, lastDate }.Distinct().ToList();
        }

        public IReadOnlyCollection<LocalDate> GetLongLeadTimeAllocationDates()
        {
            var lastShortLeadTimeAllocationDate = this.GetShortLeadTimeAllocationDates().Last();

            var firstDate = this.GetNextWorkingDayStrictlyAfter(lastShortLeadTimeAllocationDate);

            var lastDate = GetCurrentDate().Next(IsoDayOfWeek.Thursday).PlusWeeks(1).PlusDays(1);

            return this.WorkingDatesBetween(firstDate, lastDate);
        }

        public IReadOnlyCollection<LocalDate> GetWeeklyNotificationDates()
        {
            var lastDate = GetLongLeadTimeAllocationDates().Last();

            var firstDate = lastDate.Previous(IsoDayOfWeek.Monday);

            return this.WorkingDatesBetween(firstDate, lastDate);
        }

        public LocalDate GetNextWorkingDate() => GetNextWorkingDayStrictlyAfter(this.GetCurrentDate());

        private LocalDate GetNextWorkingDayIncluding(LocalDate localDate)
        {
            while (!this.IsWorkingDay(localDate))
            {
                localDate = localDate.PlusDays(1);
            }

            return localDate;
        }

        private LocalDate GetNextWorkingDayStrictlyAfter(LocalDate localDate) =>
            this.GetNextWorkingDayIncluding(localDate.PlusDays(1));

        private LocalDate GetCurrentDate() => this.GetCurrentTime().Date;

        private ZonedDateTime GetCurrentTime() => this.CurrentInstant.InZone(LondonTimeZone);

        private IReadOnlyCollection<LocalDate> WorkingDatesBetween(LocalDate firstDate, LocalDate lastDate) =>
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
