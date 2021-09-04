namespace Parking.Business.UnitTests
{
    using System.Threading.Tasks;
    using Business.EmailTemplates;
    using Data;
    using Model;
    using Moq;
    using NodaTime;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using Xunit;

    public static class AllocationNotifierTests
    {
        [Fact]
        public static async Task Notifies_each_user_with_newly_allocated_requests()
        {
            var users = new[]
            {
                CreateUser.With(userId: "user1", emailAddress: "1@abc.com"),
                CreateUser.With(userId: "user2", emailAddress: "2@xyz.co.uk"),
            };

            var mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);
            mockUserRepository
                .Setup(r => r.GetUsers())
                .ReturnsAsync(users);

            var requests = new[]
            {
                new Request("user1", 11.January(2021), RequestStatus.Allocated),
                new Request("user2", 12.January(2021), RequestStatus.Allocated)
            };

            var mockEmailRepository = new Mock<IEmailRepository>();

            var allocationNotifier = new AllocationNotifier(
                CreateDummyDateCalculator(),
                mockEmailRepository.Object,
                CreateDummyScheduleRepository(),
                mockUserRepository.Object);

            await allocationNotifier.Notify(requests);

            mockEmailRepository.Verify(
                r => r.Send(It.Is<IEmailTemplate>(t =>
                    t.To == "1@abc.com" &&
                    t.Subject == "Parking space allocated for Mon 11 Jan")),
                Times.Once);
            mockEmailRepository.Verify(
                r => r.Send(It.Is<IEmailTemplate>(t =>
                    t.To == "2@xyz.co.uk" &&
                    t.Subject == "Parking space allocated for Tue 12 Jan")),
                Times.Once);

            mockEmailRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public static async Task Groups_notifications_into_a_single_email_when_a_user_has_multiple_notifications()
        {
            var users = new[] { CreateUser.With(userId: "user1", emailAddress: "1@abc.com") };

            var mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);
            mockUserRepository
                .Setup(r => r.GetUsers())
                .ReturnsAsync(users);

            var requests = new[]
            {
                new Request("user1", 11.January(2021), RequestStatus.Allocated),
                new Request("user1", 12.January(2021), RequestStatus.Allocated)
            };

            var mockEmailRepository = new Mock<IEmailRepository>();

            var allocationNotifier = new AllocationNotifier(
                CreateDummyDateCalculator(),
                mockEmailRepository.Object,
                CreateDummyScheduleRepository(),
                mockUserRepository.Object);

            await allocationNotifier.Notify(requests);

            mockEmailRepository.Verify(
                r => r.Send(It.Is<IEmailTemplate>(t =>
                    t.To == "1@abc.com" &&
                    t.Subject == "Parking spaces allocated for multiple upcoming dates" &&
                    t.PlainTextBody.Contains("Mon 11 Jan") &&
                    t.PlainTextBody.Contains("Tue 12 Jan"))),
                Times.Once);

            mockEmailRepository.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(RequestStatus.Cancelled)]
        [InlineData(RequestStatus.HardInterrupted)]
        [InlineData(RequestStatus.Interrupted)]
        [InlineData(RequestStatus.Pending)]
        [InlineData(RequestStatus.SoftInterrupted)]
        public static async Task Does_not_notify_user_for_other_request_statuses(RequestStatus requestStatus)
        {
            var users = new[] { CreateUser.With(userId: "user1", emailAddress: "1@abc.com") };

            var mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);
            mockUserRepository
                .Setup(r => r.GetUsers())
                .ReturnsAsync(users);

            var requests = new[]
            {
                new Request("user1", 11.January(2021), requestStatus),
            };

            var mockEmailRepository = new Mock<IEmailRepository>();

            var allocationNotifier = new AllocationNotifier(
                CreateDummyDateCalculator(),
                mockEmailRepository.Object,
                CreateDummyScheduleRepository(),
                mockUserRepository.Object);

            await allocationNotifier.Notify(requests);

            mockEmailRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public static async Task Does_not_notify_user_when_request_will_appear_on_scheduled_daily_notification()
        {
            var users = new[] { CreateUser.With(userId: "user1", emailAddress: "1@abc.com") };

            var mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);
            mockUserRepository
                .Setup(r => r.GetUsers())
                .ReturnsAsync(users);

            var requests = new[] { new Request("user1", 11.January(2021), RequestStatus.Allocated) };

            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator
                .Setup(c => c.ScheduleIsDue(
                    It.Is<Schedule>(s => s.ScheduledTaskType == ScheduledTaskType.DailyNotification), 
                    Duration.FromMinutes(2)))
                .Returns(true);
            mockDateCalculator
                .Setup(c => c.ScheduleIsDue(
                    It.Is<Schedule>(s => s.ScheduledTaskType == ScheduledTaskType.WeeklyNotification),
                    Duration.FromMinutes(2)))
                .Returns(false);
            mockDateCalculator
                .Setup(c => c.GetNextWorkingDate())
                .Returns(11.January(2021));

            var mockEmailRepository = new Mock<IEmailRepository>();

            var allocationNotifier = new AllocationNotifier(
                mockDateCalculator.Object,
                mockEmailRepository.Object,
                CreateDummyScheduleRepository(),
                mockUserRepository.Object);

            await allocationNotifier.Notify(requests);

            mockEmailRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public static async Task Does_not_notify_user_when_request_will_appear_on_scheduled_weekly_notification()
        {
            var users = new[] { CreateUser.With(userId: "user1", emailAddress: "1@abc.com") };

            var mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);
            mockUserRepository
                .Setup(r => r.GetUsers())
                .ReturnsAsync(users);

            var requests = new[]
            {
                new Request("user1", 11.January(2021), RequestStatus.Allocated),
                new Request("user1", 12.January(2021), RequestStatus.Allocated)
            };

            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator
                .Setup(c => c.ScheduleIsDue(
                    It.Is<Schedule>(s => s.ScheduledTaskType == ScheduledTaskType.DailyNotification),
                    Duration.FromMinutes(2)))
                .Returns(false);
            mockDateCalculator
                .Setup(c => c.ScheduleIsDue(
                    It.Is<Schedule>(s => s.ScheduledTaskType == ScheduledTaskType.WeeklyNotification),
                    Duration.FromMinutes(2)))
                .Returns(true);
            mockDateCalculator
                .Setup(c => c.GetWeeklyNotificationDates())
                .Returns(new[] { 11.January(2021), 12.January(2021) });

            var mockEmailRepository = new Mock<IEmailRepository>();

            var allocationNotifier = new AllocationNotifier(
                mockDateCalculator.Object,
                mockEmailRepository.Object,
                CreateDummyScheduleRepository(),
                mockUserRepository.Object);

            await allocationNotifier.Notify(requests);

            mockEmailRepository.VerifyNoOtherCalls();
        }

        private static IScheduleRepository CreateDummyScheduleRepository()
        {
            var schedules = new[]
            {
                new Schedule(ScheduledTaskType.DailyNotification, Instant.MaxValue),
                new Schedule(ScheduledTaskType.WeeklyNotification, Instant.MaxValue)
            };

            var mockScheduleRepository = new Mock<IScheduleRepository>(MockBehavior.Strict);
            mockScheduleRepository.Setup(r => r.GetSchedules()).ReturnsAsync(schedules);

            return mockScheduleRepository.Object;
        }

        private static IDateCalculator CreateDummyDateCalculator()
        {
            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator.Setup(d => d.ScheduleIsDue(It.IsAny<Schedule>(), It.IsAny<Duration>())).Returns(false);
            return mockDateCalculator.Object;
        }
    }
}