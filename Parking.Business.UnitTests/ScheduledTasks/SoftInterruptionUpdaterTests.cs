namespace Parking.Business.UnitTests.ScheduledTasks
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Business.ScheduledTasks;
    using Data;
    using Model;
    using Moq;
    using NodaTime.Testing.Extensions;
    using TestHelpers;
    using Xunit;
    using static DateCalculatorTests;

    public static class SoftInterruptionUpdaterTests
    {
        [Fact]
        public static async Task Updates_unallocated_requests_to_soft_interrupted()
        {
            var nextWorkingDate = 23.December(2020);

            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator.Setup(c => c.GetNextWorkingDate()).Returns(nextWorkingDate);

            var requests = new[]
            {
                new Request("user1", nextWorkingDate, RequestStatus.Interrupted),
                new Request("user2", nextWorkingDate, RequestStatus.Interrupted)
            };

            var mockRequestRepository = new Mock<IRequestRepository>(MockBehavior.Strict);
            mockRequestRepository
                .Setup(r => r.GetRequests(nextWorkingDate, nextWorkingDate))
                .ReturnsAsync(requests);
            mockRequestRepository
                .Setup(r => r.SaveRequests(It.IsAny<IReadOnlyCollection<Request>>()))
                .Returns(Task.CompletedTask);

            var softInterruptionUpdater = new SoftInterruptionUpdater(
                mockDateCalculator.Object,
                mockRequestRepository.Object);

            await softInterruptionUpdater.Run();

            var expectedRequests = new[]
            {
                new Request("user1", nextWorkingDate, RequestStatus.SoftInterrupted),
                new Request("user2", nextWorkingDate, RequestStatus.SoftInterrupted)
            };

            mockRequestRepository.Verify(r => r.GetRequests(nextWorkingDate, nextWorkingDate), Times.Once);
            mockRequestRepository.Verify(r => r.SaveRequests(
                    It.Is<IReadOnlyCollection<Request>>(actual => CheckRequests(expectedRequests, actual.ToList()))),
                Times.Once);
        }

        [Theory]
        [InlineData(RequestStatus.Allocated)]
        [InlineData(RequestStatus.Cancelled)]
        [InlineData(RequestStatus.SoftInterrupted)]
        [InlineData(RequestStatus.HardInterrupted)]
        public static async Task Ignores_other_request_statuses(RequestStatus requestStatus)
        {
            var nextWorkingDate = 23.December(2020);

            var mockDateCalculator = new Mock<IDateCalculator>(MockBehavior.Strict);
            mockDateCalculator.Setup(c => c.GetNextWorkingDate()).Returns(nextWorkingDate);

            var requests = new[]
            {
                new Request("user1", nextWorkingDate, requestStatus),
                new Request("user2", nextWorkingDate, requestStatus)
            };

            var mockRequestRepository = new Mock<IRequestRepository>(MockBehavior.Strict);
            mockRequestRepository
                .Setup(r => r.GetRequests(nextWorkingDate, nextWorkingDate))
                .ReturnsAsync(requests);
            mockRequestRepository
                .Setup(r => r.SaveRequests(It.IsAny<IReadOnlyCollection<Request>>()))
                .Returns(Task.CompletedTask);

            var softInterruptionUpdater = new SoftInterruptionUpdater(
                mockDateCalculator.Object,
                mockRequestRepository.Object);

            await softInterruptionUpdater.Run();

            mockRequestRepository.Verify(r => r.GetRequests(nextWorkingDate, nextWorkingDate), Times.Once);
            mockRequestRepository.Verify(
                r => r.SaveRequests(It.Is<IReadOnlyCollection<Request>>(actual => actual.Count == 0)),
                Times.Once);
        }

        [Fact]
        public static void ScheduledTaskType_returns_DailyNotification()
        {
            var softInterruptionUpdater = new SoftInterruptionUpdater(
                Mock.Of<IDateCalculator>(),
                Mock.Of<IRequestRepository>());

            Assert.Equal(ScheduledTaskType.SoftInterruptionUpdater, softInterruptionUpdater.ScheduledTaskType);
        }

        [Theory]
        [InlineData(21, 22)]
        [InlineData(24, 29)]
        public static void GetNextRunTime_returns_1102_am_on_next_working_day(int currentDay, int expectedNextDay)
        {
            var bankHolidays = new[] { 25.December(2020), 28.December(2020) };

            var dateCalculator = CreateDateCalculator(currentDay.December(2020).At(11, 2, 0).Utc(), bankHolidays);

            var actual = new SoftInterruptionUpdater(
                dateCalculator,
                Mock.Of<IRequestRepository>()).GetNextRunTime();

            var expected = expectedNextDay.December(2020).At(11, 2, 0).Utc();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void GetNextRunTime_uses_London_time_zone()
        {
            var dateCalculator = CreateDateCalculator(27.March(2020).At(11, 2, 0).Utc());

            var actual = new SoftInterruptionUpdater(
                dateCalculator,
                Mock.Of<IRequestRepository>()).GetNextRunTime();

            var expected = 30.March(2020).At(10, 2, 0).Utc();

            Assert.Equal(expected, actual);
        }

        private static bool CheckRequests(
            IReadOnlyCollection<Request> expected,
            IReadOnlyCollection<Request> actual) =>
            actual.Count == expected.Count && expected.All(e => actual.Contains(e, new RequestsComparer()));
    }
}