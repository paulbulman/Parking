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
    using IEmailTemplate = Business.EmailTemplates.IEmailTemplate;
    using static DateCalculatorTests;

    public static class RequestReminderTests
    {
        [Fact]
        public static async Task Sends_emails_to_users_with_no_upcoming_requests()
        {
            var nextWeeklyNotificationDates = new[] { 21.December(2020), 22.December(2020) };

            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator
                .Setup(d => d.GetNextWeeklyNotificationDates())
                .Returns(nextWeeklyNotificationDates);

            var mockEmailRepository = new Mock<IEmailRepository>();

            var requests = new[]
            {
                new Request("user1", 18.December(2020), RequestStatus.Allocated),
                new Request("user1", 21.December(2020), RequestStatus.Cancelled)
            };

            var mockRequestRepository = new Mock<IRequestRepository>(MockBehavior.Strict);
            mockRequestRepository
                .Setup(r => r.GetRequests(22.October(2020), 22.December(2020)))
                .ReturnsAsync(requests);

            var user = CreateUser.With(userId: "user1", emailAddress: "1@abc.com");

            var mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);
            mockUserRepository
                .Setup(r => r.GetUsers())
                .ReturnsAsync(new[] { user });

            var requestReminder = new RequestReminder(
                mockDateCalculator.Object,
                mockEmailRepository.Object,
                mockRequestRepository.Object,
                mockUserRepository.Object);

            await requestReminder.Run();

            mockEmailRepository.Verify(
                r => r.Send(It.Is<IEmailTemplate>(e =>
                    e.To == "1@abc.com" && e.Subject == "No parking requests entered for Mon 21 Dec - Tue 22 Dec")),
                Times.Once);
            mockEmailRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public static async Task Does_not_send_emails_to_users_with_reminder_disabled()
        {
            var nextWeeklyNotificationDates = new[] { 21.December(2020), 22.December(2020) };

            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator
                .Setup(d => d.GetNextWeeklyNotificationDates())
                .Returns(nextWeeklyNotificationDates);

            var mockEmailRepository = new Mock<IEmailRepository>();

            var requests = new[]
            {
                new Request("user1", 18.December(2020), RequestStatus.Allocated),
                new Request("user1", 21.December(2020), RequestStatus.Cancelled)
            };

            var mockRequestRepository = new Mock<IRequestRepository>(MockBehavior.Strict);
            mockRequestRepository
                .Setup(r => r.GetRequests(22.October(2020), 22.December(2020)))
                .ReturnsAsync(requests);

            var user = CreateUser.With(userId: "user1", emailAddress: "1@abc.com", requestReminderEnabled: false);

            var mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);
            mockUserRepository
                .Setup(r => r.GetUsers())
                .ReturnsAsync(new[] { user });

            var requestReminder = new RequestReminder(
                mockDateCalculator.Object,
                mockEmailRepository.Object,
                mockRequestRepository.Object,
                mockUserRepository.Object);

            await requestReminder.Run();

            mockEmailRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public static async Task Does_not_send_emails_to_users_with_upcoming_requests()
        {
            var nextWeeklyNotificationDates = new[] { 21.December(2020), 22.December(2020) };

            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator
                .Setup(d => d.GetNextWeeklyNotificationDates())
                .Returns(nextWeeklyNotificationDates);

            var mockEmailRepository = new Mock<IEmailRepository>();

            var requests = new[]
            {
                new Request("user1", 18.December(2020), RequestStatus.Allocated),
                new Request("user1", 21.December(2020), RequestStatus.Pending)
            };

            var mockRequestRepository = new Mock<IRequestRepository>(MockBehavior.Strict);
            mockRequestRepository
                .Setup(r => r.GetRequests(22.October(2020), 22.December(2020)))
                .ReturnsAsync(requests);

            var user = CreateUser.With(userId: "user1", emailAddress: "1@abc.com");

            var mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);
            mockUserRepository
                .Setup(r => r.GetUsers())
                .ReturnsAsync(new[] { user });

            var requestReminder = new RequestReminder(
                mockDateCalculator.Object,
                mockEmailRepository.Object,
                mockRequestRepository.Object,
                mockUserRepository.Object);

            await requestReminder.Run();

            mockEmailRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public static async Task Does_not_send_emails_to_users_with_no_recent_requests()
        {
            var nextWeeklyNotificationDates = new[] { 21.December(2020), 22.December(2020) };

            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator
                .Setup(d => d.GetNextWeeklyNotificationDates())
                .Returns(nextWeeklyNotificationDates);

            var mockEmailRepository = new Mock<IEmailRepository>();

            var requests = new[]
            {
                new Request("user1", 18.December(2020), RequestStatus.Cancelled),
            };

            var mockRequestRepository = new Mock<IRequestRepository>(MockBehavior.Strict);
            mockRequestRepository
                .Setup(r => r.GetRequests(22.October(2020), 22.December(2020)))
                .ReturnsAsync(requests);

            var users = new[]
            {
                CreateUser.With(userId: "user1", emailAddress: "1@abc.com"),
                CreateUser.With(userId: "user2", emailAddress: "2@xyxz.co.uk")
            };

            var mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);
            mockUserRepository
                .Setup(r => r.GetUsers())
                .ReturnsAsync(users);

            var requestReminder = new RequestReminder(
                mockDateCalculator.Object,
                mockEmailRepository.Object,
                mockRequestRepository.Object,
                mockUserRepository.Object);

            await requestReminder.Run();

            mockEmailRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public static void ScheduledTaskType_returns_RequestReminder()
        {
            var requestReminder = new RequestReminder(
                Mock.Of<IDateCalculator>(),
                Mock.Of<IEmailRepository>(),
                Mock.Of<IRequestRepository>(),
                Mock.Of<IUserRepository>());

            Assert.Equal(ScheduledTaskType.RequestReminder, requestReminder.ScheduledTaskType);
        }

        [Theory]
        [InlineData(22, 23)]
        [InlineData(23, 30)]
        public static void GetNextRunTime_returns_midnight_on_next_Wednesday(int currentDay, int expectedNextDay)
        {
            var dateCalculator = CreateDateCalculator(currentDay.December(2020).AtMidnight().Utc());

            var actual = new RequestReminder(
                dateCalculator,
                Mock.Of<IEmailRepository>(),
                Mock.Of<IRequestRepository>(),
                Mock.Of<IUserRepository>()).GetNextRunTime();

            var expected = expectedNextDay.December(2020).AtMidnight().Utc();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void GetNextRunTime_uses_London_time_zone()
        {
            var dateCalculator = CreateDateCalculator(26.March(2020).AtMidnight().Utc());

            var actual = new RequestReminder(
                dateCalculator,
                Mock.Of<IEmailRepository>(),
                Mock.Of<IRequestRepository>(),
                Mock.Of<IUserRepository>()).GetNextRunTime();

            var expected = 31.March(2020).At(23, 0, 0).Utc();

            Assert.Equal(expected, actual);
        }
    }
}