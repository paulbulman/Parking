namespace Parking.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Aws;
    using Business.Data;
    using Model;
    using NodaTime;
    using NodaTime.Text;

    public class ScheduleRepository : IScheduleRepository
    {
        private readonly IDatabaseProvider databaseProvider;

        private static IDictionary<ScheduledTaskType, string> RawScheduledTaskTypes =>
            new Dictionary<ScheduledTaskType, string>
            {
                {ScheduledTaskType.DailyNotification, "DAILY_NOTIFICATION"},
                {ScheduledTaskType.RequestReminder, "REQUEST_REMINDER"},
                {ScheduledTaskType.ReservationReminder, "RESERVATION_REMINDER"},
                {ScheduledTaskType.WeeklyNotification, "WEEKLY_NOTIFICATION"}
            };

        public ScheduleRepository(IDatabaseProvider databaseProvider) => this.databaseProvider = databaseProvider;

        public async Task<IReadOnlyCollection<Schedule>> GetSchedules()
        {
            var rawData = await this.databaseProvider.GetSchedules();

            if (rawData.Schedules == null)
            {
                throw new InvalidOperationException("No schedules data found.");
            }

            return rawData.Schedules
                .Select(ParseSchedule)
                .ToArray();
        }

        public async Task UpdateSchedule(Schedule schedule)
        {
            var existing = await this.GetSchedules();

            var unchanged = existing.Where(t => t.ScheduledTaskType != schedule.ScheduledTaskType);

            var updated = new List<Schedule>(unchanged)
            {
                schedule
            };

            var data = updated
                .OrderBy(t => RawScheduledTaskTypes[t.ScheduledTaskType])
                .ToDictionary(
                    t => RawScheduledTaskTypes[t.ScheduledTaskType],
                    t => InstantPattern.ExtendedIso.Format(t.NextRunTime));

            var rawItem = RawItem.CreateSchedules(data);

            await this.databaseProvider.SaveItem(rawItem);
        }

        private static Schedule ParseSchedule(KeyValuePair<string, string> rawData) =>
            new Schedule(
                ParseScheduledTaskType(rawData.Key),
                ParseNextRunTime(rawData.Value));

        private static ScheduledTaskType ParseScheduledTaskType(string rawData) => RawScheduledTaskTypes
            .Single(dictionary => dictionary.Value == rawData)
            .Key;

        private static Instant ParseNextRunTime(string rawData) => InstantPattern.ExtendedIso.Parse(rawData).Value;
    }
}