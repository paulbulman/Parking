namespace Parking.Data.UnitTests
{
    using System.Linq;
    using System.Threading.Tasks;
    using Aws;
    using TestHelpers;
    using Model;
    using Moq;
    using NodaTime.Testing.Extensions;
    using Xunit;

    public static class ScheduledTaskRepositoryTests
    {
        [Fact]
        public static async Task Converts_raw_data_to_scheduled_tasks()
        {
            var rawData =
                "{" +
                "\"DAILY_NOTIFICATION\":\"2020-12-14T11:00:00Z\"," +
                "\"REQUEST_REMINDER\":\"2020-12-16T00:00:00Z\"," +
                "\"RESERVATION_REMINDER\":\"2020-12-14T10:00:00Z\"," +
                "\"WEEKLY_NOTIFICATION\":\"2020-12-17T00:00:00Z\"" +
                "}";

            var mockStorageProvider = new Mock<IStorageProvider>(MockBehavior.Strict);

            mockStorageProvider
                .Setup(p => p.GetSchedules())
                .ReturnsAsync(rawData);

            var scheduleRepository = new ScheduleRepository(mockStorageProvider.Object);

            var result = await scheduleRepository.GetSchedules();

            var expectedSchedules = new[]
            {
                new Schedule(ScheduledTaskType.DailyNotification, 14.December(2020).At(11, 0, 0).Utc()),
                new Schedule(ScheduledTaskType.RequestReminder, 16.December(2020).AtMidnight().Utc()),
                new Schedule(ScheduledTaskType.ReservationReminder, 14.December(2020).At(10, 0, 0).Utc()),
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
            const string InitialRawData = 
                "{" +
                "\"DAILY_NOTIFICATION\":\"2020-12-14T11:00:00Z\"," +
                "\"REQUEST_REMINDER\":\"2020-12-16T00:00:00Z\"," +
                "\"RESERVATION_REMINDER\":\"2020-12-14T10:00:00Z\"," +
                "\"WEEKLY_NOTIFICATION\":\"2020-12-17T00:00:00Z\"" +
                "}";

            const string ExpectedUpdatedRawData =
                "{" +
                "\"DAILY_NOTIFICATION\":\"2020-12-14T11:00:00Z\"," +
                "\"REQUEST_REMINDER\":\"2020-12-16T00:00:00Z\"," +
                "\"RESERVATION_REMINDER\":\"2020-12-15T10:00:00Z\"," +
                "\"WEEKLY_NOTIFICATION\":\"2020-12-17T00:00:00Z\"" +
                "}";

            var mockStorageProvider = new Mock<IStorageProvider>(MockBehavior.Strict);

            mockStorageProvider
                .Setup(p => p.GetSchedules())
                .ReturnsAsync(InitialRawData);
            mockStorageProvider
                .Setup(r => r.SaveSchedules(ExpectedUpdatedRawData))
                .Returns(Task.CompletedTask);

            var scheduleRepository = new ScheduleRepository(mockStorageProvider.Object);

            var updatedSchedule = new Schedule(
                ScheduledTaskType.ReservationReminder,
                15.December(2020).At(10, 0, 0).Utc());

            await scheduleRepository.UpdateSchedule(updatedSchedule);

            mockStorageProvider.VerifyAll();
        }
    }
}