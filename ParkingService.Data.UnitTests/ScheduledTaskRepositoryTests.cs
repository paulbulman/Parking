namespace ParkingService.Data.UnitTests
{
    using System.Linq;
    using System.Threading.Tasks;
    using Business.UnitTests;
    using Model;
    using Moq;
    using NodaTime.Testing.Extensions;
    using Xunit;

    public static class ScheduledTaskRepositoryTests
    {
        [Fact]
        public static async void Converts_raw_data_to_scheduled_tasks()
        {
            var rawData =
                "{" +
                "\"DAILY_NOTIFICATION\":\"2020-12-14T11:00:00Z\"," +
                "\"REQUEST_REMINDER\":\"2020-12-16T00:00:00Z\"," +
                "\"RESERVATION_REMINDER\":\"2020-12-14T10:00:00Z\"," +
                "\"WEEKLY_NOTIFICATION\":\"2020-12-17T00:00:00Z\"" +
                "}";

            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);

            mockRawItemRepository
                .Setup(r => r.GetScheduledTasks())
                .ReturnsAsync(rawData);

            var scheduledTaskRepository = new ScheduledTaskRepository(mockRawItemRepository.Object);

            var result = await scheduledTaskRepository.GetScheduledTasks();

            var expected = new[]
            {
                new ScheduledTask(ScheduledTaskType.DailyNotification, 14.December(2020).At(11, 0, 0).Utc()),
                new ScheduledTask(ScheduledTaskType.RequestReminder, 16.December(2020).AtMidnight().Utc()),
                new ScheduledTask(ScheduledTaskType.ReservationReminder, 14.December(2020).At(10, 0, 0).Utc()),
                new ScheduledTask(ScheduledTaskType.WeeklyNotification, 17.December(2020).AtMidnight().Utc())
            };

            Assert.NotNull(result);

            Assert.Equal(expected.Length, result.Count);

            foreach (var expectedTask in expected)
            {
                Assert.Single(result, t => t.ScheduledTaskType == expectedTask.ScheduledTaskType);

                var actual = result.Single(t => t.ScheduledTaskType == expectedTask.ScheduledTaskType);

                Assert.Equal(expectedTask.NextRunTime, actual.NextRunTime);
            }
        }

        [Fact]
        public static async void Saves_combined_updated_and_existing_scheduled_tasks()
        {
            var initialRawData =
                "{" +
                "\"DAILY_NOTIFICATION\":\"2020-12-14T11:00:00Z\"," +
                "\"REQUEST_REMINDER\":\"2020-12-16T00:00:00Z\"," +
                "\"RESERVATION_REMINDER\":\"2020-12-14T10:00:00Z\"," +
                "\"WEEKLY_NOTIFICATION\":\"2020-12-17T00:00:00Z\"" +
                "}";

            var expectedUpdatedRawData =
                "{" +
                "\"DAILY_NOTIFICATION\":\"2020-12-14T11:00:00Z\"," +
                "\"REQUEST_REMINDER\":\"2020-12-16T00:00:00Z\"," +
                "\"RESERVATION_REMINDER\":\"2020-12-15T10:00:00Z\"," +
                "\"WEEKLY_NOTIFICATION\":\"2020-12-17T00:00:00Z\"" +
                "}";

            var mockRawItemRepository = new Mock<IRawItemRepository>(MockBehavior.Strict);

            mockRawItemRepository
                .Setup(r => r.GetScheduledTasks())
                .ReturnsAsync(initialRawData);
            mockRawItemRepository
                .Setup(r => r.SaveScheduledTasks(expectedUpdatedRawData))
                .Returns(Task.CompletedTask);

            var scheduledTaskRepository = new ScheduledTaskRepository(mockRawItemRepository.Object);

            var updatedTask = new ScheduledTask(
                ScheduledTaskType.ReservationReminder,
                15.December(2020).At(10, 0, 0).Utc());

            await scheduledTaskRepository.UpdateScheduledTask(updatedTask);

            mockRawItemRepository.VerifyAll();
        }
    }
}