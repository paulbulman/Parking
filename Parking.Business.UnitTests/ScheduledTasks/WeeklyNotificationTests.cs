namespace Parking.Business.UnitTests.ScheduledTasks;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Business.EmailTemplates;
using Data;
using Model;
using Moq;
using NodaTime.Testing.Extensions;
using TestHelpers;
using Xunit;
using WeeklyNotification = Business.ScheduledTasks.WeeklyNotification;
using static DateCalculatorTests;

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
            new Request("user2", weeklyNotificationDates.Last(), RequestStatus.Interrupted)
        };

        var mockRequestRepository = new Mock<IRequestRepository>(MockBehavior.Strict);
        mockRequestRepository
            .Setup(r => r.GetRequests(weeklyNotificationDates.ToDateInterval()))
            .ReturnsAsync(requests);

        var users = new[]
        {
            CreateUser.With(userId: "user1", emailAddress: "1@abc.com"),
            CreateUser.With(userId: "user2", emailAddress: "2@xyz.co.uk"),
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
            .Setup(r => r.GetRequests(weeklyNotificationDates.ToDateInterval()))
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
    public static void ScheduledTaskType_returns_WeeklyNotification()
    {
        var dailySummary = new WeeklyNotification(
            Mock.Of<IDateCalculator>(),
            Mock.Of<IEmailRepository>(),
            Mock.Of<IRequestRepository>(),
            Mock.Of<IUserRepository>());

        Assert.Equal(ScheduledTaskType.WeeklyNotification, dailySummary.ScheduledTaskType);
    }

    [Theory]
    [InlineData(23, 24)]
    [InlineData(24, 31)]
    public static void GetNextRunTime_returns_midnight_on_next_Thursday(int currentDay, int expectedNextDay)
    {
        var dateCalculator = CreateDateCalculator(currentDay.December(2020).At(0, 2, 0).Utc());

        var actual = new WeeklyNotification(
            dateCalculator,
            Mock.Of<IEmailRepository>(),
            Mock.Of<IRequestRepository>(),
            Mock.Of<IUserRepository>()).GetNextRunTime();

        var expected = expectedNextDay.December(2020).At(0, 2, 0).Utc();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public static void GetNextRunTime_uses_London_time_zone()
    {
        var dateCalculator = CreateDateCalculator(27.March(2020).At(0, 2, 0).Utc());

        var actual = new WeeklyNotification(
            dateCalculator,
            Mock.Of<IEmailRepository>(),
            Mock.Of<IRequestRepository>(),
            Mock.Of<IUserRepository>()).GetNextRunTime();

        var expected = 1.April(2020).At(23, 2, 0).Utc();

        Assert.Equal(expected, actual);
    }
}