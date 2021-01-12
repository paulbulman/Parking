namespace ParkingService.Business
{
    using System.Collections.Generic;
    using System.Linq;
    using Data;
    using Model;
    using NodaTime;

    public interface IDateCalculator
    {
        Instant InitialInstant { get; }

        IReadOnlyCollection<LocalDate> GetShortLeadTimeAllocationDates();

        IReadOnlyCollection<LocalDate> GetLongLeadTimeAllocationDates();

        IReadOnlyCollection<LocalDate> GetWeeklyNotificationDates();

        IReadOnlyCollection<LocalDate> GetNextWeeklyNotificationDates();

        LocalDate GetNextWorkingDate();

        bool ScheduleIsDue(Schedule schedule);
    }

    public class DateCalculator : IDateCalculator
    {
        public static readonly DateTimeZone LondonTimeZone = DateTimeZoneProviders.Tzdb["Europe/London"];

        private readonly IBankHolidayRepository bankHolidayRepository;

        public DateCalculator(IClock clock, IBankHolidayRepository bankHolidayRepository)
        {
            this.bankHolidayRepository = bankHolidayRepository;
            this.InitialInstant = clock.GetCurrentInstant();
        }

        public Instant InitialInstant { get; }

        public IReadOnlyCollection<LocalDate> GetShortLeadTimeAllocationDates()
        {
            var initialTime = this.GetInitialTime();

            var firstDate = this.GetNextWorkingDayIncluding(initialTime.Date);

            var lastDate = firstDate == initialTime.Date && initialTime.Hour >= 11 ?
                this.GetNextWorkingDayStrictlyAfter(firstDate) :
                firstDate;

            return new[] { firstDate, lastDate }.Distinct().ToList();
        }

        public IReadOnlyCollection<LocalDate> GetLongLeadTimeAllocationDates()
        {
            var lastShortLeadTimeAllocationDate = this.GetShortLeadTimeAllocationDates().Last();

            var firstDate = this.GetNextWorkingDayStrictlyAfter(lastShortLeadTimeAllocationDate);

            var lastDate = GetInitialDate().Next(IsoDayOfWeek.Thursday).PlusWeeks(1).PlusDays(1);

            return this.WorkingDatesBetween(firstDate, lastDate);
        }

        public IReadOnlyCollection<LocalDate> GetWeeklyNotificationDates()
        {
            var lastDate = GetLongLeadTimeAllocationDates().Last();

            var firstDate = lastDate.Previous(IsoDayOfWeek.Monday);

            return this.WorkingDatesBetween(firstDate, lastDate);
        }

        public IReadOnlyCollection<LocalDate> GetNextWeeklyNotificationDates()
        {
            var firstDate = GetWeeklyNotificationDates().Last().Next(IsoDayOfWeek.Monday);

            var lastDate = firstDate.Next(IsoDayOfWeek.Friday);

            return this.WorkingDatesBetween(firstDate, lastDate);
        }

        public LocalDate GetNextWorkingDate() => GetNextWorkingDayStrictlyAfter(this.GetInitialDate());

        public bool ScheduleIsDue(Schedule schedule) => schedule.NextRunTime <= this.InitialInstant;

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

        private LocalDate GetInitialDate() => this.GetInitialTime().Date;

        private ZonedDateTime GetInitialTime() => this.InitialInstant.InZone(LondonTimeZone);

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
