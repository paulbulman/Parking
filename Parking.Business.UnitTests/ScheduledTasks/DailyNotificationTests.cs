namespace Parking.Business.UnitTests.ScheduledTasks
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
    using static DateCalculatorTests;

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
        public static void ScheduledTaskType_returns_DailyNotification()
        {
            var dailyNotification = new DailyNotification(
                Mock.Of<IDateCalculator>(),
                Mock.Of<IEmailRepository>(),
                Mock.Of<IRequestRepository>(),
                Mock.Of<IUserRepository>());

            Assert.Equal(ScheduledTaskType.DailyNotification, dailyNotification.ScheduledTaskType);
        }

        [Theory]
        [InlineData(21, 22)]
        [InlineData(24, 29)]
        public static void GetNextRunTime_returns_11_am_on_next_working_day(int currentDay, int expectedNextDay)
        {
            var bankHolidays = new[] { 25.December(2020), 28.December(2020) };

            var dateCalculator = CreateDateCalculator(currentDay.December(2020).At(11, 0, 0).Utc(), bankHolidays);

            var actual = new DailyNotification(
                dateCalculator,
                Mock.Of<IEmailRepository>(),
                Mock.Of<IRequestRepository>(),
                Mock.Of<IUserRepository>()).GetNextRunTime();

            var expected = expectedNextDay.December(2020).At(11, 0, 0).Utc();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void GetNextRunTime_uses_London_time_zone()
        {
            var dateCalculator = CreateDateCalculator(27.March(2020).At(11, 0, 0).Utc());

            var actual = new DailyNotification(
                dateCalculator,
                Mock.Of<IEmailRepository>(),
                Mock.Of<IRequestRepository>(),
                Mock.Of<IUserRepository>()).GetNextRunTime();

            var expected = 30.March(2020).At(10, 0, 0).Utc();

            Assert.Equal(expected, actual);
        }
    }
}