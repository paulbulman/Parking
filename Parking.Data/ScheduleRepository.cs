namespace Parking.Data
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Business.Data;
    using Model;
    using NodaTime;
    using NodaTime.Text;

    public class ScheduleRepository : IScheduleRepository
    {
        private readonly IRawItemRepository rawItemRepository;

        private static IDictionary<ScheduledTaskType, string> RawScheduledTaskTypes =>
            new Dictionary<ScheduledTaskType, string>
            {
                {ScheduledTaskType.DailyNotification, "DAILY_NOTIFICATION"},
                {ScheduledTaskType.RequestReminder, "REQUEST_REMINDER"},
                {ScheduledTaskType.ReservationReminder, "RESERVATION_REMINDER"},
                {ScheduledTaskType.WeeklyNotification, "WEEKLY_NOTIFICATION"}
            };

        public ScheduleRepository(IRawItemRepository rawItemRepository)
        {
            this.rawItemRepository = rawItemRepository;
        }

        public async Task<IReadOnlyCollection<Schedule>> GetSchedules()
        {
            var rawData = await this.rawItemRepository.GetSchedules();

            var data = JsonSerializer.Deserialize<IDictionary<string, string>>(rawData);

            if (data == null)
            {
                throw new SerializationException("Could not deserialize raw schedule data.");
            }

            return data
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

            var rawData = JsonSerializer.Serialize(data);

            await rawItemRepository.SaveSchedules(rawData);
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