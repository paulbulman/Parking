namespace ParkingService.Business.UnitTests.ScheduledTasks
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Business.ScheduledTasks;
    using Data;
    using Model;
    using Moq;
    using NodaTime.Testing.Extensions;
    using Xunit;
    using IEmailTemplate = Business.EmailTemplates.IEmailTemplate;

    public static class DailyNotificationTests
    {
        [Fact]
        public static async Task Sends_emails_to_users_with_requests()
        {
            var nextWorkingDate = 23.December(2020);

            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator.Setup(c => c.GetNextWorkingDate()).Returns(nextWorkingDate);

            var mockEmailRepository = new Mock<IEmailRepository>();

            var requests = new[]
            {
                new Request("user1", nextWorkingDate, RequestStatus.Allocated),
                new Request("user2", nextWorkingDate, RequestStatus.Requested)
            };

            var mockRequestRepository = new Mock<IRequestRepository>(MockBehavior.Strict);
            mockRequestRepository
                .Setup(r => r.GetRequests(nextWorkingDate, nextWorkingDate))
                .ReturnsAsync(requests);

            var users = new[]
            {
                new User("user1", null, "1@abc.com"),
                new User("user2", null, "2@xyz.co.uk")
            };

            var mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);
            mockUserRepository.Setup(r => r.GetUsers()).ReturnsAsync(users);

            var dailyNotification = new DailyNotification(
                mockDateCalculator.Object,
                mockEmailRepository.Object,
                mockRequestRepository.Object,
                mockUserRepository.Object);

            await dailyNotification.Run();

            mockEmailRepository.Verify(
                r => r.Send(It.Is<IEmailTemplate>(e =>
                    e.To == "1@abc.com" && e.Subject == "Parking status for Wed 23 Dec: Allocated")),
                Times.Once);
            mockEmailRepository.Verify(
                r => r.Send(It.Is<IEmailTemplate>(e =>
                    e.To == "2@xyz.co.uk" && e.Subject == "Parking status for Wed 23 Dec: INTERRUPTED")),
                Times.Once);

            mockEmailRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public static async Task Does_not_send_emails_to_users_with_cancelled_requests()
        {
            var nextWorkingDate = 23.December(2020);

            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator.Setup(c => c.GetNextWorkingDate()).Returns(nextWorkingDate);

            var mockEmailRepository = new Mock<IEmailRepository>();

            var requests = new[] { new Request("user1", nextWorkingDate, RequestStatus.Cancelled) };

            var mockRequestRepository = new Mock<IRequestRepository>(MockBehavior.Strict);
            mockRequestRepository
                .Setup(r => r.GetRequests(nextWorkingDate, nextWorkingDate))
                .ReturnsAsync(requests);

            var mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);
            mockUserRepository.Setup(r => r.GetUsers()).ReturnsAsync(new List<User>());

            var dailyNotification = new DailyNotification(
                mockDateCalculator.Object,
                mockEmailRepository.Object,
                mockRequestRepository.Object,
                mockUserRepository.Object);

            await dailyNotification.Run();

            mockEmailRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public static void Returns_daily_summary_for_task_type()
        {
            var dailySummary = new DailyNotification(
                Mock.Of<IDateCalculator>(),
                Mock.Of<IEmailRepository>(),
                Mock.Of<IRequestRepository>(),
                Mock.Of<IUserRepository>());

            Assert.Equal(ScheduledTaskType.DailySummary, dailySummary.ScheduledTaskType);
        }
    }
}