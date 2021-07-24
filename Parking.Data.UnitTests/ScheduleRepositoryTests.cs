namespace Parking.Data.UnitTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Aws;
    using TestHelpers;
    using Model;
    using Moq;
    using NodaTime.Testing.Extensions;
    using Xunit;

    public static class ScheduleRepositoryTests
    {
        [Fact]
        public static async Task Converts_raw_data_to_scheduled_tasks()
        {
            var rawData = new Dictionary<string, string>
            {
                {"DAILY_NOTIFICATION", "2020-12-14T11:00:00Z"},
                {"REQUEST_REMINDER", "2020-12-16T00:00:00Z"},
                {"RESERVATION_REMINDER", "2020-12-14T10:00:00Z"},
                {"SOFT_INTERRUPTION_UPDATER", "2020-12-14T11:00:00Z"},
                {"WEEKLY_NOTIFICATION", "2020-12-17T00:00:00Z"}
            };

            var mockDatabaseProvider = new Mock<IDatabaseProvider>(MockBehavior.Strict);

            mockDatabaseProvider
                .Setup(p => p.GetSchedules())
                .ReturnsAsync(RawItem.CreateSchedules(rawData));

            var scheduleRepository = new ScheduleRepository(mockDatabaseProvider.Object);

            var result = await scheduleRepository.GetSchedules();

            var expectedSchedules = new[]
            {
                new Schedule(ScheduledTaskType.DailyNotification, 14.December(2020).At(11, 0, 0).Utc()),
                new Schedule(ScheduledTaskType.RequestReminder, 16.December(2020).AtMidnight().Utc()),
                new Schedule(ScheduledTaskType.ReservationReminder, 14.December(2020).At(10, 0, 0).Utc()),
                new Schedule(ScheduledTaskType.SoftInterruptionUpdater, 14.December(2020).At(11, 0, 0).Utc()),
                new Schedule(ScheduledTaskType.WeeklyNotification, 17.December(2020).AtMidnight().Utc())
            };

            Assert.NotNull(result);

            Assert.Equal(expectedSchedules.Length, result.Count);

            foreach (var expected in expectedSchedules)
            {
                Assert.Single(result, t => t.ScheduledTaskType == expected.ScheduledTaskType);

                var actual = result.Single(t => t.ScheduledTaskType == expected.ScheduledTaskType);

                Assert.Equal(expected.NextRunTime, actual.NextRunTime);
            }
        }

        [Fact]
        public static async Task Saves_combined_updated_and_existing_scheduled_tasks()
        {
            var initialRawData = new Dictionary<string, string>
            {
                {"DAILY_NOTIFICATION", "2020-12-14T11:00:00Z"},
                {"REQUEST_REMINDER", "2020-12-16T00:00:00Z"},
                {"RESERVATION_REMINDER", "2020-12-14T10:00:00Z"},
                {"SOFT_INTERRUPTION_UPDATER", "2020-12-14T11:00:00Z"},
                {"WEEKLY_NOTIFICATION", "2020-12-17T00:00:00Z"}
            };

            var expectedUpdatedRawData = new Dictionary<string, string>
            {
                {"DAILY_NOTIFICATION", "2020-12-14T11:00:00Z"},
                {"REQUEST_REMINDER", "2020-12-16T00:00:00Z"},
                {"RESERVATION_REMINDER", "2020-12-14T10:00:00Z"},
                {"SOFT_INTERRUPTION_UPDATER", "2020-12-15T11:00:00Z"},
                {"WEEKLY_NOTIFICATION", "2020-12-17T00:00:00Z"}
            };

            var mockDatabaseProvider = new Mock<IDatabaseProvider>(MockBehavior.Strict);

            mockDatabaseProvider
                .Setup(p => p.GetSchedules())
                .ReturnsAsync(RawItem.CreateSchedules(initialRawData));
            mockDatabaseProvider
                .Setup(r => r.SaveItem(It.IsAny<RawItem>()))
                .Returns(Task.CompletedTask);

            var scheduleRepository = new ScheduleRepository(mockDatabaseProvider.Object);

            var updatedSchedule = new Schedule(
                ScheduledTaskType.SoftInterruptionUpdater,
                15.December(2020).At(11, 0, 0).Utc());

            await scheduleRepository.UpdateSchedule(updatedSchedule);

            mockDatabaseProvider.Verify(
                p => p.SaveItem(It.Is<RawItem>(r => CheckRawItem(expectedUpdatedRawData, r))),
                Times.Once);
        }

        private static bool CheckRawItem(IDictionary<string, string> expected, RawItem? actual) =>
            actual?.Schedules != null && 
            actual.Schedules.Keys.Count == expected.Keys.Count && 
            expected.Keys.ToList().All(key =>
                actual.Schedules.ContainsKey(key) &&
                actual.Schedules[key] == expected[key]);
    }
}