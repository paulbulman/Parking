namespace ParkingService.Business.UnitTests.ScheduledTasks
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Business.EmailTemplates;
    using Data;
    using Model;
    using Moq;
    using NodaTime.Testing.Extensions;
    using Xunit;
    using WeeklyNotification = Business.ScheduledTasks.WeeklyNotification;

    public static class WeeklyNotificationTests
    {
        [Fact]
        public static async Task Sends_emails_to_users_with_requests()
        {
            var weeklyNotificationDates = new[] { 21.December(2020), 24.December(2020) };

            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator.Setup(c => c.GetWeeklyNotificationDates()).Returns(weeklyNotificationDates);

            var mockEmailRepository = new Mock<IEmailRepository>();

            var requests = new[]
            {
                new Request("user1", weeklyNotificationDates.First(), RequestStatus.Allocated),
                new Request("user2", weeklyNotificationDates.Last(), RequestStatus.Requested)
            };

            var mockRequestRepository = new Mock<IRequestRepository>(MockBehavior.Strict);
            mockRequestRepository
                .Setup(r => r.GetRequests(weeklyNotificationDates.First(), weeklyNotificationDates.Last()))
                .ReturnsAsync(requests);

            var users = new[]
            {
                new User("user1", null, "1@abc.com"),
                new User("user2", null, "2@xyz.co.uk")
            };

            var mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);
            mockUserRepository.Setup(r => r.GetUsers()).ReturnsAsync(users);

            var weeklyNotification = new WeeklyNotification(
                mockDateCalculator.Object,
                mockEmailRepository.Object,
                mockRequestRepository.Object,
                mockUserRepository.Object);

            await weeklyNotification.Run();

            mockEmailRepository.Verify(
                r => r.Send(It.Is<IEmailTemplate>(e =>
                    e.To == "1@abc.com" && e.Subject == "Provisional parking status for Mon 21 Dec - Thu 24 Dec")),
                Times.Once);
            mockEmailRepository.Verify(
                r => r.Send(It.Is<IEmailTemplate>(e =>
                    e.To == "2@xyz.co.uk" && e.Subject == "Provisional parking status for Mon 21 Dec - Thu 24 Dec")),
                Times.Once);

            mockEmailRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public static async Task Does_not_send_emails_to_users_with_cancelled_requests()
        {
            var weeklyNotificationDates = new[] { 21.December(2020), 24.December(2020) };

            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator.Setup(c => c.GetWeeklyNotificationDates()).Returns(weeklyNotificationDates);

            var mockEmailRepository = new Mock<IEmailRepository>();

            var requests = new[] { new Request("user1", weeklyNotificationDates.First(), RequestStatus.Cancelled) };

            var mockRequestRepository = new Mock<IRequestRepository>(MockBehavior.Strict);
            mockRequestRepository
                .Setup(r => r.GetRequests(weeklyNotificationDates.First(), weeklyNotificationDates.Last()))
                .ReturnsAsync(requests);

            var mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);
            mockUserRepository.Setup(r => r.GetUsers()).ReturnsAsync(new List<User>());

            var weeklyNotification = new WeeklyNotification(
                mockDateCalculator.Object,
                mockEmailRepository.Object,
                mockRequestRepository.Object,
                mockUserRepository.Object);

            await weeklyNotification.Run();

            mockEmailRepository.VerifyNoOtherCalls();
        }

        [Fact]
        public static void Returns_daily_summary_for_task_type()
        {
            var dailySummary = new WeeklyNotification(
                Mock.Of<IDateCalculator>(),
                Mock.Of<IEmailRepository>(),
                Mock.Of<IRequestRepository>(),
                Mock.Of<IUserRepository>());

            Assert.Equal(ScheduledTaskType.WeeklyNotification, dailySummary.ScheduledTaskType);
        }
    }
}