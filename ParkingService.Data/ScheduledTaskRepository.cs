namespace ParkingService.Data
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Model;
    using NodaTime;
    using NodaTime.Text;

    public class ScheduledTaskRepository
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

        public ScheduledTaskRepository(IRawItemRepository rawItemRepository)
        {
            this.rawItemRepository = rawItemRepository;
        }

        public async Task<IReadOnlyCollection<ScheduledTask>> GetScheduledTasks()
        {
            var rawData = await this.rawItemRepository.GetScheduledTasks();

            var data = JsonSerializer.Deserialize<IDictionary<string, string>>(rawData);

            return data
                .Select(ParseScheduledTask)
                .ToArray();
        }

        public async Task UpdateScheduledTask(ScheduledTask scheduledTask)
        {
            var existing = await this.GetScheduledTasks();

            var unchanged = existing.Where(t => t.ScheduledTaskType != scheduledTask.ScheduledTaskType);

            var updated = new List<ScheduledTask>(unchanged)
            {
                scheduledTask
            };

            var data = updated
                .OrderBy(t => RawScheduledTaskTypes[t.ScheduledTaskType])
                .ToDictionary(
                    t => RawScheduledTaskTypes[t.ScheduledTaskType],
                    t => InstantPattern.ExtendedIso.Format(t.NextRunTime));

            var rawData = JsonSerializer.Serialize(data);

            await rawItemRepository.SaveScheduledTasks(rawData);
        }

        private static ScheduledTask ParseScheduledTask(KeyValuePair<string, string> rawData) =>
            new ScheduledTask(
                ParseScheduledTaskType(rawData.Key),
                ParseNextRunTime(rawData.Value));

        private static ScheduledTaskType ParseScheduledTaskType(string rawData) => RawScheduledTaskTypes
            .Single(dictionary => dictionary.Value == rawData)
            .Key;

        private static Instant ParseNextRunTime(string rawData) => InstantPattern.ExtendedIso.Parse(rawData).Value;
    }
}