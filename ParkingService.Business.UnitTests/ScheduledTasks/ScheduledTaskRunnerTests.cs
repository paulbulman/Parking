namespace ParkingService.Business.UnitTests.ScheduledTasks
{
    using System.Threading.Tasks;
    using Business.ScheduledTasks;
    using Data;
    using Model;
    using Moq;
    using NodaTime.Testing.Extensions;
    using Xunit;

    public static class ScheduledTaskRunnerTests
    {
        [Fact]
        public static async Task Runs_tasks_that_are_due()
        {
            var currentInstant = 30.December(2020).At(12, 07, 26).Utc();

            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator.SetupGet(c => c.InitialInstant).Returns(currentInstant);

            var dueTime = currentInstant;
            var notDueTime = currentInstant.Plus(1.Seconds());

            var schedules = new[]
            {
                new Schedule(ScheduledTaskType.DailyNotification, dueTime),
                new Schedule(ScheduledTaskType.RequestReminder, notDueTime),
                new Schedule(ScheduledTaskType.ReservationReminder, notDueTime),
                new Schedule(ScheduledTaskType.WeeklyNotification, dueTime)
            };

            var mockScheduleRepository = new Mock<IScheduleRepository>(MockBehavior.Strict);
            mockScheduleRepository.Setup(r => r.GetSchedules()).ReturnsAsync(schedules);

            var mockDailyNotification = new Mock<IScheduledTask>();
            var mockRequestReminder = new Mock<IScheduledTask>();
            var mockReservationReminder = new Mock<IScheduledTask>();
            var mockWeeklyNotification = new Mock<IScheduledTask>();

            mockDailyNotification.SetupGet(s => s.ScheduledTaskType).Returns(ScheduledTaskType.DailyNotification);
            mockRequestReminder.SetupGet(s => s.ScheduledTaskType).Returns(ScheduledTaskType.RequestReminder);
            mockReservationReminder.SetupGet(s => s.ScheduledTaskType).Returns(ScheduledTaskType.ReservationReminder);
            mockWeeklyNotification.SetupGet(s => s.ScheduledTaskType).Returns(ScheduledTaskType.WeeklyNotification);

            var scheduledTasks = new[]
            {
                mockDailyNotification.Object,
                mockRequestReminder.Object,
                mockReservationReminder.Object,
                mockWeeklyNotification.Object
            };

            var scheduledTaskRunner = new ScheduledTaskRunner(
                mockDateCalculator.Object,
                scheduledTasks,
                mockScheduleRepository.Object);

            await scheduledTaskRunner.RunScheduledTasks();

            mockDailyNotification.Verify(s => s.Run(), Times.Once);
            mockRequestReminder.Verify(s => s.Run(), Times.Never);
            mockReservationReminder.Verify(s => s.Run(), Times.Never);
            mockWeeklyNotification.Verify(s => s.Run(), Times.Once);
        }
    }
}