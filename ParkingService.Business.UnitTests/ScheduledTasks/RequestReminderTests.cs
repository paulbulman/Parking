namespace ParkingService.Business.UnitTests.ScheduledTasks
{
    using System.Threading.Tasks;
    using Business.ScheduledTasks;
    using Data;
    using Model;
    using Moq;
    using NodaTime.Testing.Extensions;
    using Xunit;
    using IEmailTemplate = Business.EmailTemplates.IEmailTemplate;

    public static class RequestReminderTests
    {
        [Fact]
        public static async Task Sends_emails_to_users_with_no_upcoming_active_requests()
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

            var user = new User("user1", null, "1@abc.com");

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
        public static async Task Does_not_send_emails_to_users_with_upcoming_active_requests()
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
                new Request("user1", 21.December(2020), RequestStatus.Requested)
            };

            var mockRequestRepository = new Mock<IRequestRepository>(MockBehavior.Strict);
            mockRequestRepository
                .Setup(r => r.GetRequests(22.October(2020), 22.December(2020)))
                .ReturnsAsync(requests);

            var user = new User("user1", null, "1@abc.com");

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
        public static async Task Does_not_send_emails_to_users_with_no_recent_active_requests()
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
                new Request("user2", 21.December(2020), RequestStatus.Requested)
            };

            var mockRequestRepository = new Mock<IRequestRepository>(MockBehavior.Strict);
            mockRequestRepository
                .Setup(r => r.GetRequests(22.October(2020), 22.December(2020)))
                .ReturnsAsync(requests);

            var users = new[]
            {
                new User("user1", null, "1@abc.com"),
                new User("user2", null, "2@xyxz.co.uk")
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
        public static void Returns_request_reminder_for_task_type()
        {
            var requestReminder = new RequestReminder(
                Mock.Of<IDateCalculator>(),
                Mock.Of<IEmailRepository>(),
                Mock.Of<IRequestRepository>(),
                Mock.Of<IUserRepository>());

            Assert.Equal(ScheduledTaskType.RequestReminder, requestReminder.ScheduledTaskType);
        }
    }
}