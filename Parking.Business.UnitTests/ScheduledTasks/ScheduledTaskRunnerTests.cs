namespace Parking.Business.UnitTests.ScheduledTasks
{
    using System.Threading.Tasks;
    using Business.ScheduledTasks;
    using Data;
    using Model;
    using Moq;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using Xunit;

    public static class ScheduledTaskRunnerTests
    {
        [Fact]
        public static async Task Runs_tasks_that_are_due()
        {
            var currentInstant = 30.December(2020).At(12, 07, 26).Utc();

            var dueTime = currentInstant;
            var notDueTime = currentInstant.Plus(1.Seconds());

            var dailyNotificationSchedule = new Schedule(ScheduledTaskType.DailyNotification, dueTime);
            var requestReminderSchedule = new Schedule(ScheduledTaskType.RequestReminder, notDueTime);
            var softInterruptionUpdaterSchedule = new Schedule(ScheduledTaskType.SoftInterruptionUpdater, notDueTime);
            var weeklyNotificationSchedule = new Schedule(ScheduledTaskType.WeeklyNotification, dueTime);

            var schedules = new[]
            {
                dailyNotificationSchedule,
                requestReminderSchedule,
                softInterruptionUpdaterSchedule,
                weeklyNotificationSchedule
            };

            var mockScheduleRepository = new Mock<IScheduleRepository>(MockBehavior.Strict);
            mockScheduleRepository.Setup(r => r.GetSchedules()).ReturnsAsync(schedules);
            mockScheduleRepository.Setup(r => r.UpdateSchedule(It.IsAny<Schedule>())).Returns(Task.CompletedTask);

            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator.Setup(d => d.ScheduleIsDue(dailyNotificationSchedule, null)).Returns(true);
            mockDateCalculator.Setup(d => d.ScheduleIsDue(requestReminderSchedule, null)).Returns(false);
            mockDateCalculator.Setup(d => d.ScheduleIsDue(softInterruptionUpdaterSchedule, null)).Returns(false);
            mockDateCalculator.Setup(d => d.ScheduleIsDue(weeklyNotificationSchedule, null)).Returns(true);

            var mockDailyNotification = CreateMockScheduledTask(ScheduledTaskType.DailyNotification);
            var mockRequestReminder = CreateMockScheduledTask(ScheduledTaskType.RequestReminder);
            var mockSoftInterruptionUpdater = CreateMockScheduledTask(ScheduledTaskType.SoftInterruptionUpdater);
            var mockWeeklyNotification = CreateMockScheduledTask(ScheduledTaskType.WeeklyNotification);

            var scheduledTasks = new[]
            {
                mockDailyNotification.Object,
                mockRequestReminder.Object,
                mockSoftInterruptionUpdater.Object,
                mockWeeklyNotification.Object
            };

            var scheduledTaskRunner = new ScheduledTaskRunner(
                mockDateCalculator.Object,
                scheduledTasks,
                mockScheduleRepository.Object);

            await scheduledTaskRunner.RunScheduledTasks();

            mockDailyNotification.Verify(s => s.Run(), Times.Once);
            mockRequestReminder.Verify(s => s.Run(), Times.Never);
            mockSoftInterruptionUpdater.Verify(s => s.Run(), Times.Never);
            mockWeeklyNotification.Verify(s => s.Run(), Times.Once);
        }

        [Fact]
        public static async Task Updates_schedules_for_tasks_that_are_run()
        {
            var currentInstant = 30.December(2020).At(12, 07, 26).Utc();

            var dueTime = currentInstant;
            var notDueTime = currentInstant.Plus(1.Seconds());

            var dailyNotificationSchedule = new Schedule(ScheduledTaskType.DailyNotification, dueTime);
            var requestReminderSchedule = new Schedule(ScheduledTaskType.RequestReminder, notDueTime);
            var weeklyNotificationSchedule = new Schedule(ScheduledTaskType.WeeklyNotification, dueTime);

            var schedules = new[]
            {
                dailyNotificationSchedule,
                requestReminderSchedule,
                weeklyNotificationSchedule
            };

            var mockScheduleRepository = new Mock<IScheduleRepository>();
            mockScheduleRepository.Setup(r => r.GetSchedules()).ReturnsAsync(schedules);

            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator.Setup(d => d.ScheduleIsDue(dailyNotificationSchedule, null)).Returns(true);
            mockDateCalculator.Setup(d => d.ScheduleIsDue(requestReminderSchedule, null)).Returns(false);
            mockDateCalculator.Setup(d => d.ScheduleIsDue(weeklyNotificationSchedule, null)).Returns(true);

            var mockDailyNotification = CreateMockScheduledTask(ScheduledTaskType.DailyNotification);
            var mockRequestReminder = CreateMockScheduledTask(ScheduledTaskType.RequestReminder);
            var mockWeeklyNotification = CreateMockScheduledTask(ScheduledTaskType.WeeklyNotification);

            var dailyNotificationNextRunTime = 31.December(2020).At(11, 0, 0).Utc();
            var weeklyNotificationNextRunTime = 31.December(2020).AtMidnight().Utc();

            mockDailyNotification.Setup(s => s.GetNextRunTime()).Returns(dailyNotificationNextRunTime);
            mockWeeklyNotification.Setup(s => s.GetNextRunTime()).Returns(weeklyNotificationNextRunTime);

            var scheduledTasks = new[]
            {
                mockDailyNotification.Object,
                mockRequestReminder.Object,
                mockWeeklyNotification.Object
            };

            var scheduledTaskRunner = new ScheduledTaskRunner(
                mockDateCalculator.Object,
                scheduledTasks,
                mockScheduleRepository.Object);

            await scheduledTaskRunner.RunScheduledTasks();

            mockScheduleRepository.Verify(r => r.GetSchedules(), Times.Once);
            mockScheduleRepository.Verify(
                r => r.UpdateSchedule(It.Is<Schedule>(s =>
                    s.ScheduledTaskType == ScheduledTaskType.DailyNotification &&
                    s.NextRunTime == dailyNotificationNextRunTime)), 
                Times.Once);
            mockScheduleRepository.Verify(
                r => r.UpdateSchedule(It.Is<Schedule>(s =>
                    s.ScheduledTaskType == ScheduledTaskType.WeeklyNotification &&
                    s.NextRunTime == weeklyNotificationNextRunTime)),
                Times.Once);
            mockScheduleRepository.VerifyNoOtherCalls();
        }

        private static Mock<IScheduledTask> CreateMockScheduledTask(ScheduledTaskType scheduledTaskType)
        {
            var mockScheduledTask = new Mock<IScheduledTask>();

            mockScheduledTask.SetupGet(s => s.ScheduledTaskType).Returns(scheduledTaskType);

            return mockScheduledTask;
        }
    }
}